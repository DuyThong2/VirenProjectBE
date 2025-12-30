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
        Task<PaginatedResponse<ProductResponseDto>> GetProductsAsync(ProductRequestDto request,CancellationToken cancellationToken = default);
        //Get products by category with pagination, search, and sorting
        Task<PaginatedResponse<ProductResponseDto>> GetProductsByCateAsync(ProductRequestDto request, CancellationToken cancellationToken = default);
        //Get product and details by id
        Task<ServiceResponse> GetProductDetailAsync(Guid productId, CancellationToken cancellationToken = default);


    }
}
