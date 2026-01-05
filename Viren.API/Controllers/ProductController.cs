using Microsoft.AspNetCore.Mvc;
using Viren.Repositories.Storage.Bucket;
using Viren.Services.Dtos.Requests;
using Viren.Services.Dtos.Response;
using Viren.Services.Impl;
using Viren.Services.Interfaces;

namespace Viren.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly IS3Storage _storage;
        public ProductController(IProductService productService, IS3Storage storage)
        {
            _productService = productService;
            _storage = storage;
        }

        [HttpGet]
        public async Task<IResult> GetProductsAsync(
            [FromQuery] string? search,
            [FromQuery] string? sortBy = "CreatedAt",
            [FromQuery] string? sortDirection = "Desc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var request = new GetProductPaginatedRequest
            {
                Search = search,
                SortBy = sortBy ?? "CreatedAt",
                SortDirection = sortDirection ?? "Desc",
                Page = page,
                PageSize = pageSize
            };  

            var serviceResponse = await _productService.GetProductsAsync(request, cancellationToken);
            return serviceResponse.Succeeded
                    ? TypedResults.Ok(serviceResponse)
                    : TypedResults.BadRequest(serviceResponse);
        }

        [HttpGet]
        [Route("by-category/{categoryId:guid}")]
        public async Task<IResult> GetProductsByCateAsync(
            [FromRoute] Guid categoryId,
            [FromQuery] string? search,
            [FromQuery] string? sortBy = "CreatedAt",
            [FromQuery] string? sortDirection = "Desc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            
            CancellationToken cancellationToken = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;
            var request = new GetProductPaginatedRequest
            {
                Search = search,
                SortBy = sortBy ?? "CreatedAt",
                SortDirection = sortDirection ?? "Desc",
                CategoryId = categoryId,
                Page = page,
                PageSize = pageSize
            };
            var serviceResponse = await _productService.GetProductsByCateAsync(request, cancellationToken);
            return serviceResponse.Succeeded
                    ? TypedResults.Ok(serviceResponse)
                    : TypedResults.BadRequest(serviceResponse);
        }

        [HttpGet("{id}")]
        public async Task<IResult> GetProductDetailAsync(
            [FromRoute] Guid id,
            CancellationToken cancellationToken = default)
        {
            var serviceResponse = await _productService.GetProductDetailAsync(id, cancellationToken);
            if (serviceResponse.Succeeded)
            {
                return TypedResults.Ok(serviceResponse);
            }
            return TypedResults.BadRequest(serviceResponse);
        }

        [HttpPost]
        public async Task<IResult> CreateProductAsync(
            [FromBody] ProductRequestDto request,
            CancellationToken cancellationToken)
        {
            var serviceResponse = await _productService.CreateProductAsync(request, cancellationToken);
            if (serviceResponse.Succeeded)
            {
                return TypedResults.Ok(serviceResponse);
            }
            return TypedResults.BadRequest(serviceResponse);    
        }

        [HttpPut("{id}")]
        public async Task<IResult> UpdateProductAsync(
            [FromRoute] Guid id,
            [FromBody] ProductRequestDto request,
            CancellationToken cancellationToken)
        {
            var serviceResponse = await _productService.UpdateProductAsync(id, request, cancellationToken);
            if (serviceResponse.Succeeded)
            {
                return TypedResults.Ok(serviceResponse);
            }
            return TypedResults.BadRequest(serviceResponse);
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IResult> DeleteProductAsync(
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
        {
            var serviceResponse = await _productService.DeleteProductAsync(id, cancellationToken);
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
        public async Task<IActionResult> UploadProductFileAsync(
            [FromRoute] Guid id,
            [FromForm] UploadRequest req,
            CancellationToken cancellationToken = default)
        {
            var resp = await _productService.ReconcileProductThumbnailAsync(id, req.KeepJson, req.Files, req.Meta, cancellationToken);

            Console.WriteLine($"[User Reconcile] product={id} uploaded={resp.UploadedFiles.Count} desired={resp.Desired.Count}");

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
