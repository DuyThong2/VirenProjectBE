using Viren.Services.ApiResponse;
using Viren.Services.Dtos.Requests;
using Viren.Services.Dtos.Response;

namespace Viren.Services.Interfaces;

public interface IOrderService
{
    Task<ResponseData<Guid>> CreateOrderAsync(OrderRequestDto request, CancellationToken cancellationToken);
    Task<PaginatedResponse<OrderResponseDto>> GetOrdersAsync(GetOrderPaginatedRequest request, CancellationToken cancellationToken);
    Task<ResponseData<OrderResponseDto>> GetOrderByIdAsync(Guid id, CancellationToken ct);
    //Task<ServiceResponse> DeleteOrderAsync(Guid id, CancellationToken ct);

    Task<ServiceResponse> UpdateOrderAsync(Guid orderId, OrderRequestDto request, CancellationToken ct);
    Task<ServiceResponse> MarkOrderPaidAsync(Guid orderId, CancellationToken ct = default);
    Task<ServiceResponse> MarkOrderCancelledAsync(Guid orderId, CancellationToken ct = default);
}
