using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Viren.Services.Dtos.Requests;
using Viren.Services.Dtos.Response;
using Viren.Services.Interfaces;

namespace Viren.API.Controllers;

[ApiController]
[Route("api/meshy")]
public sealed class MeshyController : ControllerBase
{
    private readonly IMeshyService _meshyService;

    public MeshyController(IMeshyService meshyService)
    {
        _meshyService = meshyService;
    }

    [Authorize]
    [HttpPost("image-to-3d")]
    public async Task<IActionResult> CreateImageTo3DTaskAsync(
        [FromBody] MeshyImageTo3DRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            return BadRequest(new { error = "Thiếu image_url" });
        }

        try
        {
            var response = await _meshyService.CreateImageTo3DTaskAsync(request, cancellationToken);
            return ToContentResult(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("image-to-3d/{id}")]
    public async Task<IActionResult> GetImageTo3DTaskAsync(
        [FromRoute] string id,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new { error = "Thiếu task id" });
        }

        var response = await _meshyService.GetImageTo3DTaskAsync(id, cancellationToken);

        return ToContentResult(response);
    }

    [Authorize]
    [HttpGet("history/me")]
    public async Task<IActionResult> GetMyMeshyTasksAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var response = await _meshyService.GetMyMeshyTasksAsync(page, pageSize, cancellationToken);

        return response.Succeeded ? Ok(response) : Unauthorized(response);
    }

    private static IActionResult ToContentResult(MeshyProxyResponse response) =>
        new ContentResult
        {
            StatusCode = response.StatusCode,
            Content = response.Content,
            ContentType = response.ContentType
        };
}