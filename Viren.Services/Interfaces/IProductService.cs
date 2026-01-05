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
    public interface IProductService
    {
        //Get all products with pagination, search, and sorting
        Task<PaginatedResponse<ProductResponseDto>> GetProductsAsync(GetProductPaginatedRequest request,CancellationToken cancellationToken = default);
        //Get products by category with pagination, search, and sorting
        Task<PaginatedResponse<ProductResponseDto>> GetProductsByCateAsync(GetProductPaginatedRequest request, CancellationToken cancellationToken = default);
        //Get product and details by id
        Task<ServiceResponse> GetProductDetailAsync(Guid productId, CancellationToken cancellationToken = default);
        //Create a new product
        Task<ResponseData<Guid>> CreateProductAsync(ProductRequestDto request, CancellationToken cancellationToken = default);
        //Update an existing product
        Task<ServiceResponse> UpdateProductAsync(Guid id, ProductRequestDto request, CancellationToken cancellationToken = default);
        //Delete a product
        Task<ServiceResponse> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default);

        Task<ReconcileResponseDto> ReconcileProductThumbnailAsync(
            Guid productId,
            string? keepJson,
            List<IFormFile>? files,
            string? meta,
            CancellationToken ct);
    }
}
