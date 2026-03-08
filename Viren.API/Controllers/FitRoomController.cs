using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Viren.Services.Dtos.Requests;
using Viren.Services.Dtos.Response;
using Viren.Services.Interfaces;

namespace Viren.API.Controllers;

[ApiController]
[Route("api/fitroom")]
public sealed class FitRoomController : ControllerBase
{
    private readonly IFitRoomService _fitRoomService;

    public FitRoomController(IFitRoomService fitRoomService)
    {
        _fitRoomService = fitRoomService;
    }

    [HttpPost("check-model")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> CheckModelAsync(
        [FromForm] FitRoomCheckImageRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasFile(request.Image))
        {
            return BadRequest(new { error = "Thiếu file upload" });
        }

        var response = await _fitRoomService.CheckModelAsync(request.Image!, cancellationToken);
        return ToContentResult(response);
    }

    [HttpPost("check-clothes")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> CheckClothesAsync(
        [FromForm] FitRoomCheckImageRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasFile(request.Image))
        {
            return BadRequest(new { error = "Thiếu file upload" });
        }

        var response = await _fitRoomService.CheckClothesAsync(request.Image!, cancellationToken);
        return ToContentResult(response);
    }

    [HttpPost("tryon")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> CreateTryOnAsync(
        [FromForm] FitRoomTryOnRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasFile(request.ModelImage))
        {
            return BadRequest(new { error = "Thiếu hình model_image" });
        }

        try
        {
            var clothType = (request.ClothType ?? "upper").Trim().ToLowerInvariant();
            if (clothType == "combo")
            {
                if (!HasFile(request.ClothImage) || !HasFile(request.LowerClothImage))
                {
                    return BadRequest(new { error = "Thiếu cloth_image hoặc lower_cloth_image cho combo" });
                }
            }
            else if (!HasFile(request.ClothImage))
            {
                return BadRequest(new { error = "Thiếu cloth_image" });
            }

            var response = await _fitRoomService.CreateTryOnTaskAsync(request, cancellationToken);
            return ToContentResult(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("tryon/{id}")]
    public async Task<IActionResult> GetTryOnTaskAsync(
        [FromRoute] string id,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new { error = "Thiếu task id" });
        }

        var response = await _fitRoomService.GetTryOnTaskAsync(id, cancellationToken);
        return ToContentResult(response);
    }

    [Authorize]
    [HttpGet("history/me")]
    public async Task<IActionResult> GetMyHistoryAsync(
        [FromQuery] GetFitRoomHistoryRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _fitRoomService.GetMyHistoryAsync(request, cancellationToken);
        return response.Succeeded ? Ok(response) : Unauthorized(response);
    }

    private static bool HasFile(IFormFile? file) => file is { Length: > 0 };

    private static IActionResult ToContentResult(FitRoomProxyResponse response) =>
        new ContentResult
        {
            StatusCode = response.StatusCode,
            Content = response.Content,
            ContentType = response.ContentType
        };
}
