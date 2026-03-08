using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Viren.Repositories.Domains;
using Viren.Repositories.Interfaces;
using Viren.Repositories.Storage.Bucket;
using Viren.Repositories.Utils;
using Viren.Services.ApiResponse;
using Viren.Services.Dtos.Requests;
using Viren.Services.Dtos.Response;
using Viren.Services.Interfaces;

namespace Viren.Services.Impl;

public sealed class FitRoomService : IFitRoomService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IS3Storage _storage;
    private readonly IUser _currentUser;
    private readonly ILogger<FitRoomService> _logger;

    public FitRoomService(
        HttpClient httpClient,
        IUnitOfWork unitOfWork,
        IS3Storage storage,
        IUser currentUser,
        ILogger<FitRoomService> logger)
    {
        _httpClient = httpClient;
        _unitOfWork = unitOfWork;
        _storage = storage;
        _currentUser = currentUser;
        _logger = logger;
    }

    public Task<FitRoomProxyResponse> CheckModelAsync(IFormFile image, CancellationToken cancellationToken = default) =>
        ForwardSingleImageAsync("api/tryon/input_check/v1/model", image, cancellationToken);

    public Task<FitRoomProxyResponse> CheckClothesAsync(IFormFile image, CancellationToken cancellationToken = default) =>
        ForwardSingleImageAsync("api/tryon/input_check/v1/clothes", image, cancellationToken);

    public async Task<FitRoomProxyResponse> CreateTryOnTaskAsync(FitRoomTryOnRequest request, CancellationToken cancellationToken = default)
    {
        var clothType = NormalizeClothType(request.ClothType);
        var hdMode = ParseHdMode(request.HdMode);

        var filesToUpload = new List<IFormFile>
        {
            request.ModelImage!
        };

        if (request.ClothImage is not null)
        {
            filesToUpload.Add(request.ClothImage);
        }

        if (request.LowerClothImage is not null)
        {
            filesToUpload.Add(request.LowerClothImage);
        }

        var uploadedFiles = await _storage.UploadAsync(filesToUpload, cancellationToken);
        var modelUpload = uploadedFiles[0];
        var clothUpload = request.ClothImage is not null ? uploadedFiles[1] : null;
        var lowerClothUpload = request.LowerClothImage is not null ? uploadedFiles[^1] : null;

        using var content = new MultipartFormDataContent();
        AddFile(content, "model_image", request.ModelImage!);
        if (request.ClothImage is not null)
        {
            AddFile(content, "cloth_image", request.ClothImage);
        }
        if (request.LowerClothImage is not null)
        {
            AddFile(content, "lower_cloth_image", request.LowerClothImage);
        }
        content.Add(new StringContent(clothType, Encoding.UTF8), "cloth_type");
        content.Add(new StringContent(hdMode ? "true" : "false", Encoding.UTF8), "hd_mode");

        var response = await SendAsync(HttpMethod.Post, "api/tryon/v2/tasks", content, cancellationToken);
        if (response.StatusCode < StatusCodes.Status200OK || response.StatusCode >= StatusCodes.Status300MultipleChoices)
        {
            return response;
        }

        string taskId;
        string status;
        string? resultUrl;
        int? progress;
        string? errorMessage;
        DateTime? startedAt;
        DateTime? completedAt;

        try
        {
            using var document = JsonDocument.Parse(response.Content);
            var root = document.RootElement;
            taskId = TryFindStringProperty(root, "task_id") ?? throw new InvalidOperationException("FitRoom response did not contain task_id.");
            status = TryFindStringProperty(root, "status") ?? "CREATED";
            resultUrl = TryFindStringProperty(root, "download_signed_url") ?? TryFindStringProperty(root, "result_url");
            progress = TryFindIntProperty(root, "progress");
            errorMessage = TryFindStringProperty(root, "error_message") ?? TryFindStringProperty(root, "error");
            startedAt = TryFindUnixTimeProperty(root, "started_at");
            completedAt = TryFindUnixTimeProperty(root, "completed_at")
                ?? TryFindUnixTimeProperty(root, "finished_at");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "FitRoom returned malformed JSON while creating a task.");
            return CreateJsonResponse(
                StatusCodes.Status502BadGateway,
                new { error = "FitRoom returned malformed JSON." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "FitRoom create-task response is missing required fields.");
            return CreateJsonResponse(
                StatusCodes.Status502BadGateway,
                new { error = ex.Message });
        }

        var repository = _unitOfWork.GetRepository<FitRoomTask, Guid>();
        var now = DateTime.UtcNow;
        var entity = new FitRoomTask
        {
            Id = Guid.NewGuid(),
            FitRoomTaskId = taskId,
            UserId = TryGetCurrentUserId(),
            ClothType = clothType,
            HdMode = hdMode,
            ModelImageKey = modelUpload.Key,
            ModelImageUrl = modelUpload.Url,
            ClothImageKey = clothUpload?.Key ?? string.Empty,
            ClothImageUrl = clothUpload?.Url ?? string.Empty,
            LowerClothImageKey = lowerClothUpload?.Key,
            LowerClothImageUrl = lowerClothUpload?.Url,
            Status = status,
            Progress = progress,
            ResultUrl = resultUrl,
            ErrorMessage = errorMessage,
            LatestResponseJson = response.Content,
            CreatedAt = now,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            LastSyncedAt = now
        };

        await repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<FitRoomProxyResponse> GetTryOnTaskAsync(string taskId, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(HttpMethod.Get, $"api/tryon/v2/tasks/{taskId}", null, cancellationToken);
        if (response.StatusCode < StatusCodes.Status200OK || response.StatusCode >= StatusCodes.Status300MultipleChoices)
        {
            return response;
        }

        var repository = _unitOfWork.GetRepository<FitRoomTask, Guid>();
        var entity = await repository.Query(asNoTracking: false)
            .FirstOrDefaultAsync(x => x.FitRoomTaskId == taskId, cancellationToken);

        if (entity is null)
        {
            return response;
        }

        try
        {
            using var document = JsonDocument.Parse(response.Content);
            var root = document.RootElement;
            entity.Status = TryFindStringProperty(root, "status") ?? entity.Status;
            entity.Progress = TryFindIntProperty(root, "progress") ?? entity.Progress;
            entity.ResultUrl = TryFindStringProperty(root, "download_signed_url")
                ?? TryFindStringProperty(root, "result_url")
                ?? entity.ResultUrl;
            entity.ErrorMessage = TryFindStringProperty(root, "error_message")
                ?? TryFindStringProperty(root, "error")
                ?? entity.ErrorMessage;
            entity.StartedAt = TryFindUnixTimeProperty(root, "started_at") ?? entity.StartedAt;
            entity.CompletedAt = TryFindUnixTimeProperty(root, "completed_at")
                ?? TryFindUnixTimeProperty(root, "finished_at")
                ?? entity.CompletedAt;
            entity.LatestResponseJson = response.Content;
            entity.LastSyncedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Unable to parse FitRoom task payload for local sync. taskId={TaskId}", taskId);
        }

        return response;
    }

    public async Task<PaginatedResponse<FitRoomHistoryDto>> GetMyHistoryAsync(GetFitRoomHistoryRequest request, CancellationToken cancellationToken = default)
    {
        var userId = TryGetCurrentUserId();
        if (userId is null)
        {
            return new PaginatedResponse<FitRoomHistoryDto>
            {
                Succeeded = false,
                Message = "Không xác định được người dùng."
            };
        }

        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 10 : Math.Min(request.PageSize, 100);

        var repository = _unitOfWork.GetRepository<FitRoomTask, Guid>();
        var query = repository.Query()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt);

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new FitRoomHistoryDto
            {
                Id = x.Id,
                TaskId = x.FitRoomTaskId,
                ClothType = x.ClothType,
                HdMode = x.HdMode,
                ModelImageUrl = x.ModelImageUrl,
                ClothImageUrl = x.ClothImageUrl,
                LowerClothImageUrl = x.LowerClothImageUrl,
                Status = x.Status,
                Progress = x.Progress,
                ResultUrl = x.ResultUrl,
                ErrorMessage = x.ErrorMessage,
                CreatedAt = x.CreatedAt,
                StartedAt = x.StartedAt,
                CompletedAt = x.CompletedAt,
                LastSyncedAt = x.LastSyncedAt
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<FitRoomHistoryDto>
        {
            Succeeded = true,
            Message = "Lấy lịch sử try-on thành công.",
            PageNumber = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            Data = items
        };
    }

    private async Task<FitRoomProxyResponse> ForwardSingleImageAsync(string relativePath, IFormFile image, CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();
        AddFile(content, "input_image", image);
        return await SendAsync(HttpMethod.Post, relativePath, content, cancellationToken);
    }

    private async Task<FitRoomProxyResponse> SendAsync(
        HttpMethod method,
        string relativePath,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, relativePath)
        {
            Content = content
        };

        try
        {
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return new FitRoomProxyResponse
            {
                StatusCode = (int)response.StatusCode,
                Content = string.IsNullOrWhiteSpace(body) ? "{}" : body,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json"
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("FitRoom request timed out. method={Method} path={Path}", method, relativePath);
            return CreateJsonResponse(
                StatusCodes.Status502BadGateway,
                new { error = "FitRoom request timed out." });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "FitRoom request failed. method={Method} path={Path}", method, relativePath);
            return CreateJsonResponse(
                StatusCodes.Status502BadGateway,
                new { error = "Không thể kết nối tới FitRoom." });
        }
    }

    private static void AddFile(MultipartFormDataContent content, string fieldName, IFormFile file)
    {
        var streamContent = new StreamContent(file.OpenReadStream());
        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
        }

        content.Add(streamContent, fieldName, file.FileName);
    }

    private static string NormalizeClothType(string? clothType)
    {
        var normalized = (clothType ?? "upper").Trim().ToLowerInvariant();
        return normalized switch
        {
            "upper" => "upper",
            "lower" => "lower",
            "full" => "full_set",
            "full_set" => "full_set",
            "combo" => "combo",
            _ => throw new ArgumentException("cloth_type phải là upper, lower, full, full_set hoặc combo.")
        };
    }

    private static bool ParseHdMode(string? hdMode)
    {
        if (string.IsNullOrWhiteSpace(hdMode))
        {
            return true;
        }

        if (bool.TryParse(hdMode, out var value))
        {
            return value;
        }

        throw new ArgumentException("hd_mode phải là true hoặc false.");
    }

    private Guid? TryGetCurrentUserId() =>
        Guid.TryParse(_currentUser.Id, out var userId) ? userId : null;

    private static FitRoomProxyResponse CreateJsonResponse(int statusCode, object payload) =>
        new()
        {
            StatusCode = statusCode,
            Content = JsonSerializer.Serialize(payload, JsonOptions),
            ContentType = "application/json"
        };

    private static string? TryFindStringProperty(JsonElement element, string propertyName)
    {
        if (!TryFindPropertyValue(element, propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.True => bool.TrueString.ToLowerInvariant(),
            JsonValueKind.False => bool.FalseString.ToLowerInvariant(),
            _ => null
        };
    }

    private static int? TryFindIntProperty(JsonElement element, string propertyName)
    {
        if (!TryFindPropertyValue(element, propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var intValue))
        {
            return intValue;
        }

        return value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed)
            ? parsed
            : null;
    }

    private static DateTime? TryFindUnixTimeProperty(JsonElement element, string propertyName)
    {
        if (!TryFindPropertyValue(element, propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var unixSeconds))
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
        }

        if (value.ValueKind == JsonValueKind.String && long.TryParse(value.GetString(), out var parsed))
        {
            return DateTimeOffset.FromUnixTimeSeconds(parsed).UtcDateTime;
        }

        return null;
    }

    private static bool TryFindPropertyValue(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }

                if (TryFindPropertyValue(property.Value, propertyName, out value))
                {
                    return true;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (TryFindPropertyValue(item, propertyName, out value))
                {
                    return true;
                }
            }
        }

        value = default;
        return false;
    }
}
