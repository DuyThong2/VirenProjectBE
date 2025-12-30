using Viren.Services.ApiResponse;

namespace Viren.Services.Interfaces;

public interface IOrderService
{
    Task<ServiceResponse> MarkOrderPaidAsync(Guid orderId, CancellationToken ct = default);
    Task<ServiceResponse> MarkOrderCancelledAsync(Guid orderId, CancellationToken ct = default);
}
