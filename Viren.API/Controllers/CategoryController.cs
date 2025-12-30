using Microsoft.AspNetCore.Mvc;
using Viren.Services.Dtos.Requests;
using Viren.Services.Interfaces;

namespace Viren.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : Controller
    {
        private readonly ICategoryService _categoryService;
        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<IResult> GetAllCategoryAsync(
            [FromQuery] GetCategoryPaginatedRequest request,
            CancellationToken cancellationToken = default)
        {
            var serviceResponse = await _categoryService.GetAllCategoryAsync(request, cancellationToken);
            return TypedResults.Ok(serviceResponse);
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

        [HttpPut]
        public async Task<IResult> UpdateCategoryAsync(
            [FromBody] CategoryRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var serviceResponse = await _categoryService.UpdateCategoryAsync(request, cancellationToken);
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
    }
}
