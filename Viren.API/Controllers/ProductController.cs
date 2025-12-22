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
            [FromQuery] ProductRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var serviceResponse = await _productService.GetProductsAsync(request, cancellationToken);
            return TypedResults.Ok(serviceResponse);
        }

        [HttpGet]
        [Route("by-category")]
        public async Task<IResult> GetProductsByCateAsync(
            [FromQuery] ProductRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var serviceResponse = await _productService.GetProductsByCateAsync(request, cancellationToken);
            return TypedResults.Ok(serviceResponse);
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
