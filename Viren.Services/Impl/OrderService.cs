using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading;
using Viren.Repositories.Domains;
using Viren.Repositories.Enums;
using Viren.Repositories.Interfaces;
using Viren.Repositories.Utils;
using Viren.Services.ApiResponse;
using Viren.Services.Dtos.Requests;
using Viren.Services.Dtos.Response;
using Viren.Services.Interfaces;

namespace Viren.Services.Impl;

public sealed class OrderService : IOrderService
{
    private const string ErrOrderNotFound = "Không tìm thấy đơn hàng.";
    private const string ErrInvalidOrderStatus = "Trạng thái đơn hàng không hợp lệ.";
    private const string ErrInvalidStatusTransition = "Không thể chuyển trạng thái đơn hàng theo loại thanh toán đã chọn.";

    private static readonly HashSet<(OrderStatus From, OrderStatus To)> CodTransitions =
    [
        (OrderStatus.Pending, OrderStatus.Shipping),
        (OrderStatus.Shipping, OrderStatus.Completed)
    ];

    private static readonly HashSet<(OrderStatus From, OrderStatus To)> OnlineTransitions =
    [
        (OrderStatus.Pending, OrderStatus.Paid),
        (OrderStatus.Paid, OrderStatus.Shipping),
        (OrderStatus.Shipping, OrderStatus.Completed)
    ];

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IUnitOfWork unitOfWork, ILogger<OrderService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResponse> MarkOrderPaidAsync(Guid orderId, CancellationToken ct = default)
    {
        var orderRepo = _unitOfWork.GetRepository<Order, Guid>();

        var order = await orderRepo.Query()
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order == null)
        {
            return new ServiceResponse { Succeeded = false, Message = ErrOrderNotFound };
        }

        if (order.Status != OrderStatus.Pending)
        {
            return new ServiceResponse { Succeeded = false, Message = ErrInvalidOrderStatus };
        }

        try
        {
            order.Status = OrderStatus.Paid;

            if (order.Payment != null)
            {
                order.Payment.Status = PaymentStatus.Success;
                order.Payment.VerifiedAt = TimeConverter.GetCurrentVietNamTime().DateTime;
            }

            await orderRepo.UpdateAsync(order, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Order {OrderId} marked as Paid", orderId);

            return new ServiceResponse { Succeeded = true, Message = "Thanh toán đơn hàng thành công." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark order paid {OrderId}", orderId);

            if (order.Payment != null)
            {
                order.Payment.Status = PaymentStatus.Failed;
            }

            await _unitOfWork.SaveChangesAsync(ct);

            return new ServiceResponse { Succeeded = false, Message = "Xử lý thanh toán thất bại." };
        }
    }

    public async Task<ServiceResponse> MarkOrderCancelledAsync(Guid orderId, CancellationToken ct = default)
    {
        var orderRepo = _unitOfWork.GetRepository<Order, Guid>();

        var order = await orderRepo.Query()
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order == null)
        {
            return new ServiceResponse { Succeeded = false, Message = ErrOrderNotFound };
        }

        if (order.Status != OrderStatus.Pending)
        {
            return new ServiceResponse { Succeeded = false, Message = ErrInvalidOrderStatus };
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

            return new ServiceResponse { Succeeded = true, Message = "Đơn hàng đã bị hủy." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel order {OrderId}", orderId);

            if (order.Payment != null)
            {
                order.Payment.Status = PaymentStatus.Failed;
            }

            await _unitOfWork.SaveChangesAsync(ct);

            return new ServiceResponse { Succeeded = false, Message = "Hủy đơn hàng thất bại." };
        }
    }

    public async Task<ServiceResponse> UpdateOrderStatusAsync(Guid orderId, UpdateOrderStatusRequestDto request, CancellationToken ct = default)
    {
        var orderRepo = _unitOfWork.GetRepository<Order, Guid>();
        var paymentRepo = _unitOfWork.GetRepository<Payment, Guid>();

        var order = await orderRepo.Query()
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order == null)
        {
            return new ServiceResponse { Succeeded = false, Message = ErrOrderNotFound };
        }

        if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Completed)
        {
            return new ServiceResponse { Succeeded = false, Message = ErrInvalidOrderStatus };
        }

        if (!IsValidStatusTransition(order.Status, request.TargetStatus, request.PaymentType))
        {
            return new ServiceResponse { Succeeded = false, Message = ErrInvalidStatusTransition };
        }

        try
        {
            order.Status = request.TargetStatus;

            if (request.PaymentType == PaymentType.Cod)
            {
                if (order.Payment == null)
                {
                    order.Payment = new Payment
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        PaymentType = PaymentType.Cod,
                        Amount = order.TotalAmount,
                        Status = request.TargetStatus == OrderStatus.Paid ? PaymentStatus.Success : PaymentStatus.Pending,
                        CreatedAt = DateTime.UtcNow,
                        VerifiedAt = request.TargetStatus == OrderStatus.Paid
                            ? TimeConverter.GetCurrentVietNamTime().DateTime
                            : null,
                        UserId = order.UserId
                    };

                    await paymentRepo.AddAsync(order.Payment, ct);
                }
                else
                {
                    order.Payment.PaymentType = PaymentType.Cod;
                    order.Payment.Amount = order.TotalAmount;
                    order.Payment.Status = request.TargetStatus == OrderStatus.Paid ? PaymentStatus.Success : PaymentStatus.Pending;
                    order.Payment.VerifiedAt = request.TargetStatus == OrderStatus.Paid
                        ? TimeConverter.GetCurrentVietNamTime().DateTime
                        : null;

                    await paymentRepo.UpdateAsync(order.Payment, ct);
                }
            }
            else if (request.PaymentType == PaymentType.PayOs && request.TargetStatus == OrderStatus.Paid)
            {
                if (order.Payment == null)
                {
                    return new ServiceResponse
                    {
                        Succeeded = false,
                        Message = "Đơn online chưa có thông tin payment, không thể chuyển Paid."
                    };
                }

                order.Payment.PaymentType = PaymentType.PayOs;
                order.Payment.Amount = order.TotalAmount;
                order.Payment.Status = PaymentStatus.Success;
                order.Payment.VerifiedAt = TimeConverter.GetCurrentVietNamTime().DateTime;

                await paymentRepo.UpdateAsync(order.Payment, ct);
            }

            await orderRepo.UpdateAsync(order, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return new ServiceResponse
            {
                Succeeded = true,
                Message = "Cập nhật trạng thái đơn hàng thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to update order status. OrderId={OrderId}, To={ToStatus}, PaymentType={PaymentType}",
                orderId, request.TargetStatus, request.PaymentType);

            return new ServiceResponse
            {
                Succeeded = false,
                Message = "Cập nhật trạng thái đơn hàng thất bại."
            };
        }
    }

    private static bool IsValidStatusTransition(OrderStatus currentStatus, OrderStatus targetStatus, PaymentType paymentType)
    {
        if (currentStatus == targetStatus)
        {
            return true;
        }

        if (targetStatus == OrderStatus.Cancelled)
        {
            return currentStatus != OrderStatus.Completed;
        }

        return paymentType switch
        {
            PaymentType.Cod => CodTransitions.Contains((currentStatus, targetStatus)),
            PaymentType.PayOs => OnlineTransitions.Contains((currentStatus, targetStatus)),
            _ => false
        };
    }

    public async Task<ResponseData<Guid>> CreateOrderAsync(OrderRequestDto request, CancellationToken cancellationToken)
    {
        if (request.Items == null || request.Items.Count == 0)
        {
            return new ResponseData<Guid>
            {
                Succeeded = false,
                Message = "Đơn hàng phải có ít nhất một sản phẩm"
            };
        }

        var orderRepo = _unitOfWork.GetRepository<Order, Guid>();
        var orderItemRepo = _unitOfWork.GetRepository<OrderItem, Guid>();
        var productDetailRepo = _unitOfWork.GetRepository<ProductDetail, Guid>();

        var now = DateTime.UtcNow;
        decimal totalAmount = 0;

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            ShippingAddress = request.ShippingAddress.Trim(),
            Note = request.Note?.Trim(),
            Status = OrderStatus.Pending,
            CreatedAt = now,
            TotalAmount = 0
        };

        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
            {
                return new ResponseData<Guid>
                {
                    Succeeded = false,
                    Message = "Số lượng sản phẩm không hợp lệ"
                };
            }

            var productDetail = await productDetailRepo
                .Query()
                .AsTracking()
                .Include(x => x.Product)
                .FirstOrDefaultAsync(
                    x => x.Id == item.ProductDetailId,
                    cancellationToken);

            if (productDetail == null)
            {
                return new ResponseData<Guid>
                {
                    Succeeded = false,
                    Message = "Sản phẩm không tồn tại"
                };
            }

            if (productDetail.Stock < item.Quantity)
            {
                return new ResponseData<Guid>
                {
                    Succeeded = false,
                    Message = "Số lượng sản phẩm trong kho không đủ"
                };
            }

            var price = productDetail.Product.Price;
            var lineTotal = price * item.Quantity;

            productDetail.Stock -= item.Quantity;

            totalAmount += lineTotal;

            await orderItemRepo.AddAsync(new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductDetailId = item.ProductDetailId,
                Quantity = item.Quantity,
                Price = price
            }, cancellationToken);
        }

        order.TotalAmount = totalAmount;

        await orderRepo.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ResponseData<Guid>
        {
            Succeeded = true,
            Message = "Tạo đơn hàng thành công",
            Data = order.Id
        };
    }

    public async Task<PaginatedResponse<OrderResponseDto>> GetOrdersAsync(GetOrderPaginatedRequest request, CancellationToken cancellationToken)
    {
        var orderRepo = _unitOfWork.GetRepository<Order, Guid>();

        var query = orderRepo
            .Query()
            .AsNoTracking();

        if (request.StatusFilter.HasValue)
        {
            query = query.Where(o => o.Status == request.StatusFilter);
        }

        if (request.UserId != Guid.Empty)
        {
            query = query.Where(o => o.UserId == request.UserId);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.Trim().ToLower();
            query = query.Where(o =>
                o.ShippingAddress.ToLower().Contains(searchLower)
            );
        }

        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            bool ascending = string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            query = request.SortBy.ToLower() switch
            {
                "totalamount" => ascending ? query.OrderBy(p => p.TotalAmount) : query.OrderByDescending(p => p.TotalAmount),
                "status" => ascending ? query.OrderBy(p => p.Status) : query.OrderByDescending(p => p.Status),
                "createdat" => ascending ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt),
                _ => query.OrderByDescending(p => p.CreatedAt),
            };
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var orders = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Include(o => o.OrderItems)
            .Select(o => new OrderResponseDto
            {
                Id = o.Id,
                TotalAmount = o.TotalAmount,
                ShippingAddress = o.ShippingAddress,
                Note = o.Note,
                CreatedAt = o.CreatedAt,
                Status = o.Status,
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<OrderResponseDto>
        {
            Succeeded = true,
            Message = "Lấy danh sách đơn hàng thành công",
            PageNumber = request.Page,
            PageSize = request.PageSize,
            TotalItems = totalCount,
            Data = orders
        };
    }

    public async Task<ResponseData<OrderResponseDto>> GetOrderByIdAsync(Guid id, CancellationToken ct)
    {
        var orderRepo = _unitOfWork.GetRepository<Order, Guid>();

        var order = await orderRepo.Query()
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.ProductDetail)
                    .ThenInclude(pd => pd.Product)
            .Where(o => o.Id == id)
            .Select(o => new OrderResponseDto
            {
                Id = o.Id,
                UserName = o.User.Name,
                TotalAmount = o.TotalAmount,
                ShippingAddress = o.ShippingAddress,
                Note = o.Note,
                CreatedAt = o.CreatedAt,
                Status = o.Status,
                Items = o.OrderItems.Select(oi => new OrderItemResponseDto
                {
                    ProductDetailId = oi.ProductDetailId,
                    ProductName = oi.ProductDetail.Product.Name,
                    Images = oi.ProductDetail.Images,
                    Color = oi.ProductDetail.Color,
                    Size = oi.ProductDetail.Size,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.Price
                }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (order == null)
        {
            return new ResponseData<OrderResponseDto>
            {
                Succeeded = false,
                Message = ErrOrderNotFound
            };
        }

        return new ResponseData<OrderResponseDto>
        {
            Succeeded = true,
            Message = "Lấy thông tin đơn hàng thành công.",
            Data = order
        };
    }

    public async Task<ServiceResponse> UpdateOrderAsync(Guid id, OrderRequestDto request, CancellationToken ct)
    {
        var orderRepo = _unitOfWork.GetRepository<Order, Guid>();

        var order = orderRepo
            .Query()
            .AsTracking()
            .FirstOrDefault(o => o.Id == id);

        if (order == null)
        {
            return new ServiceResponse
            {
                Succeeded = false,
                Message = ErrOrderNotFound
            };
        }

        order.ShippingAddress = request.ShippingAddress.Trim();
        order.Note = request.Note?.Trim();
        await _unitOfWork.SaveChangesAsync(ct);

        return new ServiceResponse
        {
            Succeeded = true,
            Message = "Cập nhật đơn hàng thành công."
        };
    }
}
