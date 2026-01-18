using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Services.ApiResponse;
using Viren.Services.Dtos.Requests;
using Viren.Services.Dtos.Response;


namespace Viren.Services.Interfaces
{
    public interface ICategoryService
    {
        // Get all 
        Task<PaginatedResponse<CategoryResponseDto>> GetAllCategoryAsync(GetCategoryPaginatedRequest request, CancellationToken cancellationToken = default);
        //Get only name
        Task<ServiceResponse> GetCateName(CancellationToken cancellationToken = default);
        // Create 
        Task<ResponseData<Guid>> CreateCategoryAsync(CategoryRequestDto request, CancellationToken cancellationToken = default);
        // Get by id
        Task<ResponseData<CategoryResponseDto>> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default);
        // Update
        Task<ServiceResponse> UpdateCategoryAsync(Guid id, CategoryRequestDto request, CancellationToken cancellationToken = default);
        // Delete
        Task<ServiceResponse> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default);
        // Gắn ảnh
        Task<ReconcileResponseDto> ReconcileCategoryThumbnailAsync(
            Guid categoryId,
            string? keepJson,
            List<IFormFile>? files,
            string? meta,
            CancellationToken ct);
    }
}
