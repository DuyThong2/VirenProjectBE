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
        // Create 
        Task<ResponseData<Guid>> CreateCategoryAsync(CategoryRequestDto request, CancellationToken cancellationToken = default);
        // Get by id
        Task<ResponseData<CategoryResponseDto>> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default);
        // Update
        Task<ServiceResponse> UpdateCategoryAsync(CategoryRequestDto request, CancellationToken cancellationToken = default);
        // Delete
        Task<ServiceResponse> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
