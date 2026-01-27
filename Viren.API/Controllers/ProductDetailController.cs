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
    public class ProductDetailController : ControllerBase
    {
        private IProductDetailService _productDetailService;
        private readonly IS3Storage _storage;

        public ProductDetailController(IProductDetailService productDetailService, IS3Storage storage)
        {
            _productDetailService = productDetailService;
            _storage = storage;
        }

        [HttpGet]
        public async Task<IResult> GetAllProductDetailAsync(
            [FromQuery] Guid? productId,
            [FromQuery] string? search,
            [FromQuery] string? sortBy = "size",
            [FromQuery] string? sortDirection = "desc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;
            var request = new GetProductDetailPaginatedRequest
            {
                Search = search,
                SortBy = sortBy ?? "size",
                SortDirection = sortDirection ?? "desc",
                PageNumber = page,
                PageSize = pageSize,
                ProductId = productId
            };
            var serviceResponse = await _productDetailService.GetAllProductDetailAsync(request, cancellationToken);
            return TypedResults.Ok(serviceResponse);
        }

        [HttpGet("by-order")]
        public async Task<IResult> GetProductDetailByOrderIdAsync(
            [FromQuery] Guid OrderId,
            [FromQuery] string? search,
            [FromQuery] string? sortBy = "size",
            [FromQuery] string? sortDirection = "desc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;
            var request = new GetProductDetailPaginatedRequest
            {
                Search = search,
                SortBy = sortBy ?? "size",
                SortDirection = sortDirection ?? "desc",
                PageNumber = page,
                PageSize = pageSize,
            };
            var serviceResponse = await _productDetailService.GetProductDetailByOrderId(OrderId, request, cancellationToken);
            return TypedResults.Ok(serviceResponse);
        }

        [HttpPost]
        public async Task<IResult> CreateProductDetailAsync(
            [FromBody] ProductDetailRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var serviceResponse = await _productDetailService.CreateProductDetailAsync(request, cancellationToken);
            if (serviceResponse.Succeeded)
            {
                return TypedResults.Ok(serviceResponse);
            }
            return TypedResults.BadRequest(serviceResponse);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IResult> GetProductDetailByIdAsync(
            [FromRoute] Guid id,
            CancellationToken cancellationToken = default)
        {
            var serviceResponse = await _productDetailService.GetProductDetailByIdAsync(id, cancellationToken);
            if (serviceResponse.Succeeded)
            {
                return TypedResults.Ok(serviceResponse);
            }
            return TypedResults.NotFound(serviceResponse);
        }

        [HttpPut("{id}")]
        public async Task<IResult> UpdateProductDetailAsync(
            [FromRoute] Guid id,
            [FromBody] ProductDetailRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var serviceResponse = await _productDetailService.UpdateProductDetailAsync(id, request, cancellationToken);
            if (serviceResponse.Succeeded)
            {
                return TypedResults.Ok(serviceResponse);
            }
            return TypedResults.BadRequest(serviceResponse);
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IResult> DeleteProductDetailAsync(
            [FromRoute] Guid id,
            CancellationToken cancellationToken = default)
        {
            var serviceResponse = await _productDetailService.DeleteProductDetailAsync(id, cancellationToken);
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
        public async Task<IActionResult> UploadProductDetailFileAsync(
            [FromRoute] Guid id,
            [FromForm] UploadRequest req,
            CancellationToken cancellationToken = default)
        {
            var resp = await _productDetailService.ReconcileProductDetailThumbnailAsync(id, req.KeepJson, req.Files, req.Meta, cancellationToken);

            Console.WriteLine($"[User Reconcile] ProductDetail={id} uploaded={resp.UploadedFiles.Count} desired={resp.Desired.Count}");

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
