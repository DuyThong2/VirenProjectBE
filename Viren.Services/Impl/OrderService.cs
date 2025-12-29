using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Viren.Repositories.Domains;
using Viren.Repositories.Enums;
using Viren.Repositories.Interfaces;
using Viren.Repositories.Utils;
using Viren.Services.ApiResponse;
using Viren.Services.Interfaces;

namespace Viren.Services.Impl;

public sealed class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IUnitOfWork unitOfWork, ILogger<OrderService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Đánh dấu đơn hàng đã thanh toán thành công
    /// (được gọi từ PaymentService / Webhook)
    /// </summary>
    public async Task<ServiceResponse> MarkOrderPaidAsync(Guid orderId, CancellationToken ct = default)
    {
        var orderRepo = _unitOfWork.GetRepository<Order, Guid>();

        var order = await orderRepo.Query()
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order == null)
        {
            return new ServiceResponse
            {
                Succeeded = false,
                Message = "Không tìm thấy đơn hàng."
            };
        }

        if (order.Status != OrderStatus.Pending)
        {
            return new ServiceResponse
            {
                Succeeded = false,
                Message = "Trạng thái đơn hàng không hợp lệ."
            };
        }

        try
        {
            // 1️⃣ Update Order
            order.Status = OrderStatus.Paid;

            // 2️⃣ Update Payment (nếu có)
            if (order.Payment != null)
            {
                order.Payment.Status = PaymentStatus.Success;
                order.Payment.VerifiedAt = TimeConverter.GetCurrentVietNamTime().DateTime;
            }

            await orderRepo.UpdateAsync(order, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Order {OrderId} marked as Paid", orderId);

            return new ServiceResponse
            {
                Succeeded = true,
                Message = "Thanh toán đơn hàng thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark order paid {OrderId}", orderId);

            if (order.Payment != null)
                order.Payment.Status = PaymentStatus.Failed;

            await _unitOfWork.SaveChangesAsync(ct);

            return new ServiceResponse
            {
                Succeeded = false,
                Message = "Xử lý thanh toán thất bại."
            };
        }
    }

    /// <summary>
    /// Đánh dấu đơn hàng bị hủy
    /// </summary>
    public async Task<ServiceResponse> MarkOrderCancelledAsync(Guid orderId, CancellationToken ct = default)
    {
        var orderRepo = _unitOfWork.GetRepository<Order, Guid>();

        var order = await orderRepo.Query()
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order == null)
        {
            return new ServiceResponse
            {
                Succeeded = false,
                Message = "Không tìm thấy đơn hàng."
            };
        }

        if (order.Status != OrderStatus.Pending)
        {
            return new ServiceResponse
            {
                Succeeded = false,
                Message = "Trạng thái đơn hàng không hợp lệ."
            };
        }

        try
        {
            order.Status = OrderStatus.Cancelled;

            if (order.Payment != null)
            {
                order.Payment.Status = PaymentStatus.Cancelled;
                order.Payment.VerifiedAt = TimeConverter.GetCurrentVietNamTime().DateTime;
            }

            await orderRepo.UpdateAsync(order, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Order {OrderId} cancelled", orderId);

            return new ServiceResponse
            {
                Succeeded = true,
                Message = "Đơn hàng đã bị hủy."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel order {OrderId}", orderId);

            if (order.Payment != null)
                order.Payment.Status = PaymentStatus.Failed;

            await _unitOfWork.SaveChangesAsync(ct);

            return new ServiceResponse
            {
                Succeeded = false,
                Message = "Hủy đơn hàng thất bại."
            };
        }
    }
}
