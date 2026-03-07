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

        // Filter By Status
        if (request.StatusFilter.HasValue)
        {
            query = query.Where(o => o.Status == request.StatusFilter);
        }
        // Filter by UserId
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
        var OrderRepo = _unitOfWork.GetRepository<Order, Guid>();

        var order = await OrderRepo.Query()
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
                Message = "Không tìm thấy đơn hàng."
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

    public async Task<ResponseData<DashboardSummaryResponse>> GetDashboardSummaryAsync(CancellationToken ct)
    {
        var now = TimeConverter.GetCurrentVietNamTime().DateTime;

        var todayStart = now.Date;
        var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var orderRepo = _unitOfWork.GetRepository<Order, Guid>();
        var paymentRepo = _unitOfWork.GetRepository<Payment, Guid>();
        var userRepo = _unitOfWork.GetRepository<User, Guid>();
        var productRepo = _unitOfWork.GetRepository<Product, Guid>();
        var productDetailRepo = _unitOfWork.GetRepository<ProductDetail, Guid>();

        // ====================
        // Revenue (Payment)
        // ====================

        var payments = paymentRepo
            .Query()
            .AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Success);

        var revenueToday = await payments
            .Where(p => p.VerifiedAt >= todayStart)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0;

        var revenueWeek = await payments
            .Where(p => p.VerifiedAt >= weekStart)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0;

        var revenueMonth = await payments
            .Where(p => p.VerifiedAt >= monthStart)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0;

        // ====================
        // Orders
        // ====================

        var orders = orderRepo.Query().AsNoTracking();

        var ordersToday = await orders.CountAsync(o => o.CreatedAt >= todayStart, ct);
        var ordersWeek = await orders.CountAsync(o => o.CreatedAt >= weekStart, ct);
        var ordersMonth = await orders.CountAsync(o => o.CreatedAt >= monthStart, ct);

        var pendingOrders = await orders.CountAsync(o => o.Status == OrderStatus.Pending, ct);
        var paidOrders = await orders.CountAsync(o => o.Status == OrderStatus.Paid, ct);

        

        var totalCustomers = await userRepo.Query().CountAsync(ct);
        var totalProducts = await productRepo.Query().CountAsync(ct);

        var outOfStock = await productDetailRepo.Query().CountAsync(p => p.Stock == 0, ct);
        var lowStock = await productDetailRepo.Query().CountAsync(p => p.Stock > 0 && p.Stock <= 5, ct);

        var result = new DashboardSummaryResponse
        {
            RevenueToday = revenueToday,
            RevenueThisWeek = revenueWeek,
            RevenueThisMonth = revenueMonth,

            OrdersToday = ordersToday,
            OrdersThisWeek = ordersWeek,
            OrdersThisMonth = ordersMonth,

            TotalCustomers = totalCustomers,
            TotalProducts = totalProducts,

            PendingOrdersCount = pendingOrders,
            PaidButUnprocessedOrdersCount = paidOrders,

            OutOfStockCount = outOfStock,
            LowStockCount = lowStock
        };

        return new ResponseData<DashboardSummaryResponse>
        {
            Succeeded = true,
            Message = "Lấy dữ liệu dashboard thành công",
            Data = result
        };
    }

    public async Task<ResponseData<RevenueChartResponse>> GetRevenueChartAsync(string period, CancellationToken ct)
    {
        var paymentRepo = _unitOfWork.GetRepository<Payment, Guid>();

        var now = TimeConverter.GetCurrentVietNamTime().DateTime;

        var payments = paymentRepo
            .Query()
            .AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Success && p.VerifiedAt != null);

        List<RevenueChartPoint> points = new();

        switch (period.ToLower())
        {
            case "day":

                var today = now.Date;

                var dayData = await payments
                    .Where(p => p.CreatedAt.Date == today)
                    .GroupBy(p => p.CreatedAt.Hour)
                    .Select(g => new
                    {
                        Hour = g.Key,
                        Revenue = g.Sum(x => x.Amount)
                    })
                    .ToListAsync(ct);

                var dayDict = dayData.ToDictionary(x => x.Hour, x => x.Revenue);

                for (int h = 0; h < 24; h++)
                {
                    points.Add(new RevenueChartPoint
                    {
                        Label = $"{h}:00",
                        Revenue = dayDict.ContainsKey(h) ? dayDict[h] : 0
                    });
                }

                break;

            case "week":

                var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);

                var weekData = await payments
                    .Where(p => p.CreatedAt >= weekStart)
                    .GroupBy(p => p.CreatedAt.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Revenue = g.Sum(x => x.Amount)
                    })
                    .ToListAsync(ct);

                var weekDict = weekData.ToDictionary(x => x.Date, x => x.Revenue);

                for (int i = 0; i < 7; i++)
                {
                    var date = weekStart.AddDays(i);

                    points.Add(new RevenueChartPoint
                    {
                        Label = date.ToString("dd/MM"),
                        Revenue = weekDict.ContainsKey(date) ? weekDict[date] : 0
                    });
                }

                break;

            case "month":

                var monthStart = new DateTime(now.Year, now.Month, 1);
                var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);

                var monthData = await payments
                    .Where(p => p.CreatedAt >= monthStart)
                    .GroupBy(p => p.CreatedAt.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Revenue = g.Sum(x => x.Amount)
                    })
                    .ToListAsync(ct);

                var monthDict = monthData.ToDictionary(x => x.Date, x => x.Revenue);

                for (int i = 0; i < daysInMonth; i++)
                {
                    var date = monthStart.AddDays(i);

                    points.Add(new RevenueChartPoint
                    {
                        Label = date.ToString("dd/MM"),
                        Revenue = monthDict.ContainsKey(date) ? monthDict[date] : 0
                    });
                }

                break;
            case "year":

                var yearStart = new DateTime(now.Year, 1, 1);

                var yearData = await payments
                    .Where(p => p.CreatedAt >= yearStart)
                    .GroupBy(p => p.CreatedAt.Month)
                    .Select(g => new
                    {
                        Month = g.Key,
                        Revenue = g.Sum(x => x.Amount)
                    })
                    .ToListAsync(ct);

                var yearDict = yearData.ToDictionary(x => x.Month, x => x.Revenue);

                for (int m = 1; m <= 12; m++)
                {
                    points.Add(new RevenueChartPoint
                    {
                        Label = $"T{m}",
                        Revenue = yearDict.ContainsKey(m) ? yearDict[m] : 0
                    });
                }

                break;
            default:
                return new ResponseData<RevenueChartResponse>
                {
                    Succeeded = false,
                    Message = "Period không hợp lệ"
                };
        }

        return new ResponseData<RevenueChartResponse>
        {
            Succeeded = true,
            Data = new RevenueChartResponse
            {
                Period = period,
                Points = points
            }
        };
    }


    public async Task<ResponseData<OrderChartResponse>> GetOrderChartAsync(
    string period,
    CancellationToken ct)
    {
        var orderRepo = _unitOfWork.GetRepository<Order, Guid>();

        var now = TimeConverter.GetCurrentVietNamTime().DateTime;

        var orders = orderRepo
            .Query()
            .AsNoTracking();

        List<OrderChartPoint> points = new();

        switch (period.ToLower())
        {
            case "day":

                var dayStart = now.Date;
                var dayEnd = dayStart.AddDays(1);

                var dayData = await orders
                    .Where(o => o.CreatedAt >= dayStart && o.CreatedAt < dayEnd)
                    .GroupBy(o => new { Hour = o.CreatedAt.Hour, o.Status })
                    .Select(g => new
                    {
                        g.Key.Hour,
                        g.Key.Status,
                        Count = g.Count()
                    })
                    .ToListAsync(ct);

                var dayDict = dayData.ToDictionary(
                    x => (x.Hour, x.Status),
                    x => x.Count
                );

                for (int h = 0; h < 24; h++)
                {
                    points.Add(new OrderChartPoint
                    {
                        Label = $"{h}:00",

                        Pending = GetCount(dayDict, h, OrderStatus.Pending),
                        Paid = GetCount(dayDict, h, OrderStatus.Paid),
                        Shipping = GetCount(dayDict, h, OrderStatus.Shipping),
                        Completed = GetCount(dayDict, h, OrderStatus.Completed),
                        Cancelled = GetCount(dayDict, h, OrderStatus.Cancelled)
                    });
                }

                break;

            case "week":

                var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);
                var weekEnd = weekStart.AddDays(7);

                var weekData = await orders
                    .Where(o => o.CreatedAt >= weekStart && o.CreatedAt < weekEnd)
                    .GroupBy(o => new { Date = o.CreatedAt.Date, o.Status })
                    .Select(g => new
                    {
                        g.Key.Date,
                        g.Key.Status,
                        Count = g.Count()
                    })
                    .ToListAsync(ct);

                var weekDict = weekData.ToDictionary(
                    x => (x.Date, x.Status),
                    x => x.Count
                );

                for (int i = 0; i < 7; i++)
                {
                    var date = weekStart.AddDays(i);

                    points.Add(new OrderChartPoint
                    {
                        Label = date.ToString("dd/MM"),

                        Pending = GetCount(weekDict, date, OrderStatus.Pending),
                        Paid = GetCount(weekDict, date, OrderStatus.Paid),
                        Shipping = GetCount(weekDict, date, OrderStatus.Shipping),
                        Completed = GetCount(weekDict, date, OrderStatus.Completed),
                        Cancelled = GetCount(weekDict, date, OrderStatus.Cancelled)
                    });
                }

                break;

            case "month":

                var monthStart = new DateTime(now.Year, now.Month, 1);
                var monthEnd = monthStart.AddMonths(1);

                var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);

                var monthData = await orders
                    .Where(o => o.CreatedAt >= monthStart && o.CreatedAt < monthEnd)
                    .GroupBy(o => new { Date = o.CreatedAt.Date, o.Status })
                    .Select(g => new
                    {
                        g.Key.Date,
                        g.Key.Status,
                        Count = g.Count()
                    })
                    .ToListAsync(ct);

                var monthDict = monthData.ToDictionary(
                    x => (x.Date, x.Status),
                    x => x.Count
                );

                for (int i = 0; i < daysInMonth; i++)
                {
                    var date = monthStart.AddDays(i);

                    points.Add(new OrderChartPoint
                    {
                        Label = date.ToString("dd/MM"),

                        Pending = GetCount(monthDict, date, OrderStatus.Pending),
                        Paid = GetCount(monthDict, date, OrderStatus.Paid),
                        Shipping = GetCount(monthDict, date, OrderStatus.Shipping),
                        Completed = GetCount(monthDict, date, OrderStatus.Completed),
                        Cancelled = GetCount(monthDict, date, OrderStatus.Cancelled)
                    });
                }

                break;

            case "year":

                var yearStart = new DateTime(now.Year, 1, 1);
                var yearEnd = yearStart.AddYears(1);

                var yearData = await orders
                    .Where(o => o.CreatedAt >= yearStart && o.CreatedAt < yearEnd)
                    .GroupBy(o => new { Month = o.CreatedAt.Month, o.Status })
                    .Select(g => new
                    {
                        g.Key.Month,
                        g.Key.Status,
                        Count = g.Count()
                    })
                    .ToListAsync(ct);

                var yearDict = yearData.ToDictionary(
                    x => (x.Month, x.Status),
                    x => x.Count
                );

                for (int m = 1; m <= 12; m++)
                {
                    points.Add(new OrderChartPoint
                    {
                        Label = $"T{m}",

                        Pending = GetCount(yearDict, m, OrderStatus.Pending),
                        Paid = GetCount(yearDict, m, OrderStatus.Paid),
                        Shipping = GetCount(yearDict, m, OrderStatus.Shipping),
                        Completed = GetCount(yearDict, m, OrderStatus.Completed),
                        Cancelled = GetCount(yearDict, m, OrderStatus.Cancelled)
                    });
                }

                break;

            default:

                return new ResponseData<OrderChartResponse>
                {
                    Succeeded = false,
                    Message = "Period không hợp lệ"
                };
        }

        return new ResponseData<OrderChartResponse>
        {
            Succeeded = true,
            Data = new OrderChartResponse
            {
                Period = period,
                Points = points
            }
        };
    }
    private int GetCount<TKey>(
    Dictionary<(TKey Key, OrderStatus Status), int> dict,
    TKey key,
    OrderStatus status)
    {
        return dict.TryGetValue((key, status), out var count)
            ? count
            : 0;
    }



    public async Task<ResponseData<List<TopProductResponse>>> GetTopProductsAsync(int limit, CancellationToken ct)
    {
        var orderItemRepo = _unitOfWork.GetRepository<OrderItem, Guid>();

        var query = orderItemRepo
            .Query()
            .AsNoTracking()
            .Include(x => x.Order)
            .Include(x => x.ProductDetail)
                .ThenInclude(pd => pd.Product);

        var result = await query
            .Where(x => x.Order.Status == OrderStatus.Paid || x.Order.Status == OrderStatus.Completed)
            .GroupBy(x => new
            {
                x.ProductDetail.Product.Id,
                x.ProductDetail.Product.Name,
            })
            .Select(g => new TopProductResponse
            {
                ProductId = g.Key.Id,
                ProductName = g.Key.Name,

                SoldQuantity = g.Sum(x => x.Quantity),

                Revenue = g.Sum(x => x.Price * x.Quantity),

                StockQuantity = g.Sum(x => x.ProductDetail.Stock)
            })
            .OrderByDescending(x => x.SoldQuantity)
            .Take(limit)
            .ToListAsync(ct);

        return new ResponseData<List<TopProductResponse>>
        {
            Succeeded = true,
            Message = "Lấy danh sách sản phẩm bán chạy thành công",
            Data = result
        };
    }

    public async Task<ResponseData<List<DelayedOrderResponse>>> GetDelayedOrdersAsync(int limit, CancellationToken ct)
    {
        var orderRepo = _unitOfWork.GetRepository<Order, Guid>();

        var now = TimeConverter.GetCurrentVietNamTime().DateTime;

        var pendingThreshold = now.AddHours(-24);
        var paidThreshold = now.AddHours(-12);

        var orders = await orderRepo
            .Query()
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.Payment)
            .Where(o =>
                (o.Status == OrderStatus.Pending && o.CreatedAt <= pendingThreshold)
                ||
                (o.Status == OrderStatus.Paid &&
                 o.Payment != null &&
                 o.Payment.Status == PaymentStatus.Success &&
                 o.Payment.VerifiedAt <= paidThreshold))
            .OrderBy(o => o.CreatedAt)
            .Take(limit)
            .Select(o => new DelayedOrderResponse
            {
                OrderId = o.Id,
                OrderCode = o.Id.ToString(),

                CustomerName = o.User.Name,

                TotalAmount = o.TotalAmount,

                OrderStatus = o.Status.ToString(),

                PaymentStatus = o.Payment != null
                    ? o.Payment.Status.ToString()
                    : "None",

                DelayedType =
                    o.Status == OrderStatus.Pending
                    ? "pending_too_long"
                    : "paid_unprocessed_too_long",

                DelayedHours = (int)(now - o.CreatedAt).TotalHours,

                CreatedAt = o.CreatedAt,

                PaidAt = o.Payment != null
                    ? o.Payment.VerifiedAt
                    : null
            })
            .ToListAsync(ct);

        return new ResponseData<List<DelayedOrderResponse>>
        {
            Succeeded = true,
            Message = "Lấy danh sách đơn hàng chậm xử lý thành công",
            Data = orders
        };
    }

    public async Task<ResponseData<List<StockAlertResponse>>> GetStockAlertsAsync(int limit, CancellationToken ct)
    {
        var productDetailRepo = _unitOfWork.GetRepository<ProductDetail, Guid>();

        const int threshold = 5;

        var result = await productDetailRepo
            .Query()
            .AsNoTracking()
            .Include(pd => pd.Product)
            .Where(pd => pd.Stock <= threshold)
            .OrderBy(pd => pd.Stock)
            .Take(limit)
            .Select(pd => new StockAlertResponse
            {
                ProductId = pd.Product.Id,

                ProductName = pd.Product.Name,



                StockQuantity = pd.Stock,

                Threshold = threshold,

                Status = pd.Stock == 0
                    ? "out_of_stock"
                    : "low_stock"
            })
            .ToListAsync(ct);

        return new ResponseData<List<StockAlertResponse>>
        {
            Succeeded = true,
            Message = "Lấy danh sách cảnh báo tồn kho thành công",
            Data = result
        };
    }
    public async Task<ResponseData<List<TopCustomerResponse>>> GetTopCustomersAsync(int limit, CancellationToken ct)
    {
        var paymentRepo = _unitOfWork.GetRepository<Payment, Guid>();

        var result = await paymentRepo
            .Query()
            .AsNoTracking()
            .Include(p => p.User)
            .Where(p => p.Status == PaymentStatus.Success && p.UserId != null)
            .GroupBy(p => new
            {
                p.UserId,
                p.User!.Name,
                p.User.Email,
                p.User.PhoneNumber
            })
            .Select(g => new TopCustomerResponse
            {
                UserId = g.Key.UserId!.Value,

                CustomerName = g.Key.Name,

                Email = g.Key.Email,

                PhoneNumber = g.Key.PhoneNumber,

                TotalOrders = g.Count(),

                TotalSpent = g.Sum(x => x.Amount),

                LastOrderAt = g.Max(x => x.VerifiedAt)
            })
            .OrderByDescending(x => x.TotalSpent)
            .Take(limit)
            .ToListAsync(ct);

        return new ResponseData<List<TopCustomerResponse>>
        {
            Succeeded = true,
            Message = "Lấy danh sách khách hàng mua nhiều nhất thành công",
            Data = result
        };
    }

}
