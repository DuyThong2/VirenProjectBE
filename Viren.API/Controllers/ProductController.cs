using Microsoft.AspNetCore.Mvc;
using Viren.Services.Dtos.Requests;
using Viren.Services.Interfaces;

namespace Viren.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController
    {
        private readonly IProductService _productService;
        public ProductController(IProductService productService)
        {
            _productService = productService;
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

            var request = new ProductRequestDto
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
            var request = new ProductRequestDto
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

        [HttpGet("{productId}")]
        public async Task<IResult> GetProductDetailAsync(
            [FromRoute] Guid productId,
            CancellationToken cancellationToken = default)
        {
            var serviceResponse = await _productService.GetProductDetailAsync(productId, cancellationToken);
            if (serviceResponse.Succeeded)
            {
                return TypedResults.Ok(serviceResponse);
            }
            return TypedResults.BadRequest(serviceResponse);
        }
    }
}
