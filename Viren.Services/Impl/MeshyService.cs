using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;
using Viren.Repositories.Domains;
using Viren.Repositories.Interfaces;
using Viren.Repositories.Storage.Bucket;
using Viren.Repositories.Utils;
using Viren.Services.ApiResponse;
using Viren.Services.Dtos.Requests;
using Viren.Services.Dtos.Response;
using Viren.Services.Interfaces;

namespace Viren.Services.Impl;

public sealed class MeshyService : IMeshyService
{
    private readonly HttpClient _httpClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IS3Storage _storage;
    private readonly IUser _currentUser;
    private readonly ILogger<MeshyService> _logger;

    public MeshyService(
        HttpClient httpClient,
        IUnitOfWork unitOfWork,
        IS3Storage storage,
        IUser currentUser,
        ILogger<MeshyService> logger)
    {
        _httpClient = httpClient;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _storage = storage;
        _logger = logger;
    }

    public async Task<MeshyProxyResponse> CreateImageTo3DTaskAsync(
        MeshyImageTo3DRequest request,
        CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["image_url"] = request.ImageUrl
        };

        if (request.ModelType != null) payload["model_type"] = request.ModelType;
        if (request.AiModel != null) payload["ai_model"] = request.AiModel;
        if (request.Topology != null) payload["topology"] = request.Topology;
        if (request.TargetPolycount.HasValue) payload["target_polycount"] = request.TargetPolycount;
        if (request.ShouldRemesh.HasValue) payload["should_remesh"] = request.ShouldRemesh;
        if (request.ShouldTexture.HasValue) payload["should_texture"] = request.ShouldTexture;
        if (request.EnablePbr.HasValue) payload["enable_pbr"] = request.EnablePbr;
        if (request.RemoveLighting.HasValue) payload["remove_lighting"] = request.RemoveLighting;
        if (request.ImageEnhancement.HasValue) payload["image_enhancement"] = request.ImageEnhancement;

        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        var response = await SendAsync(
            HttpMethod.Post,
            "openapi/v1/image-to-3d",
            content,
            cancellationToken);

        if (response.StatusCode < 200 || response.StatusCode >= 300)
            return response;

        string taskId;

        try
        {
            using var doc = JsonDocument.Parse(response.Content);

            taskId = doc.RootElement
                .GetProperty("result")
                .GetString()
                ?? throw new InvalidOperationException("Meshy response missing task id.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Meshy create task parse error.");
            throw;
        }

        var repo = _unitOfWork.GetRepository<MeshyTask, Guid>();
        var fitRoomRepo = _unitOfWork.GetRepository<FitRoomTask, Guid>();

        var fitRoomTask = await fitRoomRepo.Query(asNoTracking: false)
            .FirstOrDefaultAsync(x => x.FitRoomTaskId == request.FitRoomTaskId, cancellationToken);

        if (fitRoomTask == null)
        {
            _logger.LogError("FitRoomTask with id {FitRoomTaskId} not found.", request.FitRoomTaskId);
            throw new InvalidOperationException("FitRoomTask not found.");
        }


        var entity = new MeshyTask
        {
            Id = Guid.NewGuid(),
            FitRoomTaskId = fitRoomTask.Id,
            MeshyTaskId = taskId,
            Status = "PENDING",
            Progress = 0,
            CreatedAt = DateTime.UtcNow,
            LastSyncedAt = DateTime.UtcNow,
            LatestResponseJson = response.Content
        };

        await repo.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<MeshyProxyResponse> GetImageTo3DTaskAsync(
        string meshyTaskId,
        CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(
        HttpMethod.Get,
        $"openapi/v1/image-to-3d/{meshyTaskId}",
        null,
        cancellationToken);

        if (response.StatusCode < 200 || response.StatusCode >= 300)
            return response;

        var repo = _unitOfWork.GetRepository<MeshyTask, Guid>();
        var entity = await repo.Query(false)
            .FirstOrDefaultAsync(x => x.MeshyTaskId == meshyTaskId, cancellationToken);

        if (entity == null)
            return response;

        try
        {
            using var doc = JsonDocument.Parse(response.Content);
            var root = doc.RootElement;

            entity.Status = TryGetString(root, "status") ?? entity.Status;
            entity.Progress = TryGetInt(root, "progress") ?? entity.Progress;

            if (root.TryGetProperty("model_urls", out var models))
            {
                entity.ModelGlbUrl = TryGetString(models, "glb");
                entity.ModelFbxUrl = TryGetString(models, "fbx");
                entity.ModelObjUrl = TryGetString(models, "obj");
                entity.ModelUsdzUrl = TryGetString(models, "usdz");
            }

            entity.ThumbnailUrl = TryGetString(root, "thumbnail_url");

            if (root.TryGetProperty("texture_urls", out var textures)
                && textures.ValueKind == JsonValueKind.Array
                && textures.GetArrayLength() > 0)
            {
                var tex = textures[0];
                entity.TextureBaseColorUrl = TryGetString(tex, "base_color");
                entity.TextureMetallicUrl = TryGetString(tex, "metallic");
                entity.TextureNormalUrl = TryGetString(tex, "normal");
                entity.TextureRoughnessUrl = TryGetString(tex, "roughness");
            }

            entity.LatestResponseJson = response.Content;
            entity.LastSyncedAt = DateTime.UtcNow;

            if (entity.Status == "SUCCEEDED")
            {
                entity.CompletedAt = DateTime.UtcNow;

                var rawGlbUrl = entity.ModelGlbUrl;
                var alreadyStored = !string.IsNullOrWhiteSpace(rawGlbUrl)
                                    && rawGlbUrl.StartsWith(_storage.PublicBaseUrl, StringComparison.OrdinalIgnoreCase);

                if (!string.IsNullOrWhiteSpace(rawGlbUrl) && !alreadyStored)
                {
                    try
                    {
                        var uploaded = await _storage.Upload3DAsync(rawGlbUrl, cancellationToken);
                        var stableGlbUrl = uploaded.FirstOrDefault() ?? rawGlbUrl;

                        entity.ModelGlbUrl = stableGlbUrl;

                        _logger.LogInformation(
                            "Meshy task {MeshyTaskId} .glb uploaded to S3: {StableUrl}",
                            meshyTaskId, stableGlbUrl);

                        // Patch response JSON: thay model_urls.glb bằng S3 URL
                        // để frontend nhận được stable URL ngay, không cần gọi lại API
                        response = PatchGlbUrlInResponse(response, rawGlbUrl, stableGlbUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed to upload Meshy .glb to S3 for taskId={MeshyTaskId}. Keeping raw URL.",
                            meshyTaskId);
                    }
                }
                else if (alreadyStored)
                {
                    // GLB đã được lưu S3 từ lần poll trước → patch response với stable URL luôn
                    response = PatchGlbUrlInResponse(response, rawGlbUrl!, entity.ModelGlbUrl!);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Meshy task parse failed.");
        }

        return response;
    }

    public async Task<PaginatedResponse<MeshyTaskHistoryDto>> GetMyMeshyTasksAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var userId = TryGetCurrentUserId();

        if (userId == null)
        {
            return new PaginatedResponse<MeshyTaskHistoryDto>
            {
                Succeeded = false,
                Message = "Không xác định được người dùng."
            };
        }

        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 100);

        var meshyRepo = _unitOfWork.GetRepository<MeshyTask, Guid>();
        var fitroomRepo = _unitOfWork.GetRepository<FitRoomTask, Guid>();

        var query =
            from m in meshyRepo.Query()
            join f in fitroomRepo.Query()
                on m.FitRoomTaskId equals f.Id
            where f.UserId == userId
            orderby m.CreatedAt descending
            select new MeshyTaskHistoryDto
            {
                Id = m.Id,
                MeshyTaskId = m.MeshyTaskId,
                Status = m.Status,
                Progress = m.Progress,
                ModelGlbUrl = m.ModelGlbUrl,
                ThumbnailUrl = m.ThumbnailUrl,
                CreatedAt = m.CreatedAt,
                CompletedAt = m.CompletedAt
            };

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<MeshyTaskHistoryDto>
        {
            Succeeded = true,
            Message = "Lấy danh sách Meshy task thành công.",
            PageNumber = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            Data = items
        };
    }

    private async Task<MeshyProxyResponse> SendAsync(
        HttpMethod method,
        string path,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path)
        {
            Content = content
        };

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            return new MeshyProxyResponse
            {
                StatusCode = (int)response.StatusCode,
                Content = string.IsNullOrWhiteSpace(body) ? "{}" : body,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json"
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Meshy request failed.");

            return new MeshyProxyResponse
            {
                StatusCode = (int)HttpStatusCode.BadGateway,
                Content = JsonSerializer.Serialize(new { error = "Meshy request failed." }),
                ContentType = "application/json"
            };
        }
    }

    private Guid? TryGetCurrentUserId() =>
        Guid.TryParse(_currentUser.Id, out var id) ? id : null;

    private static string? TryGetString(JsonElement element, string name)
    {
        if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            return value.GetString();

        return null;
    }

    private static int? TryGetInt(JsonElement element, string name)
    {
        if (element.TryGetProperty(name, out var value) && value.TryGetInt32(out var i))
            return i;

        return null;
    }

    private static MeshyProxyResponse PatchGlbUrlInResponse(
    MeshyProxyResponse response,
    string oldGlbUrl,
    string newGlbUrl)
    {
        if (string.IsNullOrWhiteSpace(oldGlbUrl) || string.IsNullOrWhiteSpace(newGlbUrl))
            return response;

        var patched = response.Content.Replace(oldGlbUrl, newGlbUrl, StringComparison.Ordinal);

        return new MeshyProxyResponse
        {
            StatusCode = response.StatusCode,
            ContentType = response.ContentType,
            Content = patched
        };
    }
}