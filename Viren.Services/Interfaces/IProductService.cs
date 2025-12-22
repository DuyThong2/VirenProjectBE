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
        Task<PaginatedResponse<ProductResponseDto>> GetProductsAsync(ProductRequestDto request,CancellationToken cancellationToken = default);
        Task<PaginatedResponse<ProductResponseDto>> GetProductsByCateAsync(ProductRequestDto request, CancellationToken cancellationToken = default);
        Task<ServiceResponse> GetProductDetailAsync(Guid productId, CancellationToken cancellationToken = default);
    }
}
