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
    public interface IProductDetailService
    {
        // Get all 
        Task<PaginatedResponse<ProductDetailResponseDto>> GetAllProductDetailAsync(GetProductDetailPaginatedRequest request, CancellationToken cancellationToken = default);
        // Create 
        Task<ResponseData<Guid>> CreateProductDetailAsync(ProductDetailRequestDto request, CancellationToken cancellationToken = default);
        // Get by id
        Task<ResponseData<ProductDetailResponseDto>> GetProductDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);
        // Update
        Task<ServiceResponse> UpdateProductDetailAsync(Guid id, ProductDetailRequestDto request, CancellationToken cancellationToken = default);
        // Delete
        Task<ServiceResponse> DeleteProductDetailAsync(Guid id, CancellationToken cancellationToken = default);

        Task<ReconcileResponseDto> ReconcileProductDetailThumbnailAsync(
            Guid productDetailId,
            string? keepJson,
            List<IFormFile>? files,
            string? meta,
            CancellationToken ct);
    }
}
