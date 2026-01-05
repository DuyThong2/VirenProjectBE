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

    public async Task<ResponseData<Guid>> CreateOrderAsync(CreateOrderRequestDto request, CancellationToken cancellationToken)
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

            // snapshot giá tại thời điểm đặt hàng
            var price = productDetail.Product.Price;
            var lineTotal = price * item.Quantity;

            // trừ kho
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

        // Filter by UserId
        if (request.UserId != Guid.Empty)
        {
            query = query.Where(o => o.UserId == request.UserId);
        }

        if(!string.IsNullOrWhiteSpace(request.Search))
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
        var OrderRepo = _unitOfWork.GetRepository<Order, Guid>();

        var order = await OrderRepo.Query()
            .AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new OrderResponseDto
            {
                Id = o.Id,
                TotalAmount = o.TotalAmount,
                ShippingAddress = o.ShippingAddress,
                Note = o.Note,
                CreatedAt = o.CreatedAt,
                Status = o.Status,
                Items = o.OrderItems.Select(oi => new OrderItemResponseDto
                {
                    ProductDetailId = oi.ProductDetailId,
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
                Message = "Không tìm thấy đơn hàng."
            };
        }

        return new ResponseData<OrderResponseDto>
        {
            Succeeded = true,
            Message = "Lấy thông tin đơn hàng thành công.",
            Data = order
        };
    }
}
