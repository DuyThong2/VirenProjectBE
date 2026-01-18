using Microsoft.AspNetCore.Mvc;
using Viren.Repositories.Domains;
using Viren.Repositories.Storage.Bucket;
using Viren.Services.Dtos.Requests;
using Viren.Services.Dtos.Response;
using Viren.Services.Impl;
using Viren.Services.Interfaces;

namespace Viren.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : Controller
    {
        private readonly ICategoryService _categoryService;
        private readonly IS3Storage _storage;
        public CategoryController(ICategoryService categoryService, IS3Storage storage)
        {
            _categoryService = categoryService;
            _storage = storage;
        }

        [HttpGet]
        public async Task<IResult> GetAllCategoryAsync(
            [FromQuery] string? search,
            [FromQuery] int? page = 1,
            [FromQuery] int? pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var request = new GetCategoryPaginatedRequest
            {
                Search = search,
                Page = page ?? 1,
                PageSize = pageSize ?? 10
            };

            var serviceResponse = await _categoryService.GetAllCategoryAsync(request, cancellationToken);
            return TypedResults.Ok(serviceResponse);
        }
        [HttpGet("only-name")]
        public async Task<IResult> GetCategoryByNameAsync(
            CancellationToken cancellationToken = default)
        {
            var serviceResponse = await _categoryService.GetCateName(cancellationToken);
            if (serviceResponse.Succeeded)
            {
                return TypedResults.Ok(serviceResponse);
            }
            return TypedResults.BadRequest(serviceResponse);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IResult> GetCategoryByIdAsync(
            [FromRoute] Guid id,
            CancellationToken cancellationToken = default)
        {
            var serviceResponse = await _categoryService.GetCategoryByIdAsync(id, cancellationToken);
            if (serviceResponse.Succeeded)
            {
                return TypedResults.Ok(serviceResponse);
            }
            return TypedResults.BadRequest(serviceResponse);
        }

        [HttpPost]
        public async Task<IResult> CreateCategoryAsync(
            [FromBody] CategoryRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var serviceResponse = await _categoryService.CreateCategoryAsync(request, cancellationToken);
            if (serviceResponse.Succeeded)
            {
                return TypedResults.Ok(serviceResponse);
            }
            return TypedResults.BadRequest(serviceResponse);
        }

        [HttpPut("{id}")]
        public async Task<IResult> UpdateCategoryAsync(
            [FromRoute] Guid id,
            [FromBody] CategoryRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var serviceResponse = await _categoryService.UpdateCategoryAsync(id, request, cancellationToken);
            if (serviceResponse.Succeeded)
            {
                return TypedResults.Ok(serviceResponse);
            }
            return TypedResults.BadRequest(serviceResponse);
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IResult> DeleteCategoryAsync(
            [FromRoute] Guid id,
            CancellationToken cancellationToken = default)
        {
            var serviceResponse = await _categoryService.DeleteCategoryAsync(id, cancellationToken);
            if (serviceResponse.Succeeded)
            {
                return TypedResults.Ok(serviceResponse);
            }
            return TypedResults.BadRequest(serviceResponse);
        }

        [HttpPost("{id:guid}/files")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(100 * 1024 * 1024)]
        [ProducesResponseType(typeof(ReconcileResponseDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> UploadCategoryFileAsync(
            [FromRoute] Guid id,
            [FromForm] UploadRequest req,
            CancellationToken cancellationToken = default)
        {
            var resp = await _categoryService.ReconcileCategoryThumbnailAsync(id, req.KeepJson, req.Files, req.Meta, cancellationToken);

            Console.WriteLine($"[User Reconcile] cate={id} uploaded={resp.UploadedFiles.Count} desired={resp.Desired.Count}");

            return Created(String.Empty, resp);
        }

        [HttpGet("download/{**key}")]
        public async Task<IActionResult> Download([FromRoute] string key, [FromQuery] string? filename)
        {
            var file = await _storage.DownloadAsync(key);
            if (!string.IsNullOrWhiteSpace(filename))
                file.FileDownloadName = filename;
            return file;
        }
    }
}
