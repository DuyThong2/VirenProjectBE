using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.payOS;
using Net.payOS.Types;
using Viren.Repositories.Domains;
using Viren.Repositories.Enums;
using Viren.Repositories.Interfaces;
using Viren.Repositories.Utils;
using Viren.Services.ApiResponse;
using Viren.Services.Configs;
using Viren.Services.Dtos.Requests;
using Viren.Services.Dtos.Response;
using Viren.Services.Interfaces;

// PayOS namespaces (theo SDK bạn đang dùng)


namespace Viren.Services.Impl;

public sealed class PaymentService : IPaymentService
{
    private readonly PayOS _payOs;
    private readonly PayOsSetings _payOsSetings;
    private readonly ILogger<PaymentService> _logger;
    private readonly IUnitOfWork _unitOfWork;


    public PaymentService(IUnitOfWork unitOfWork,
        ILogger<PaymentService> logger,
        IOptions<PayOsSetings> payOsSetings)
    {
        _logger = logger;
        _payOsSetings = payOsSetings.Value;
        _unitOfWork = unitOfWork;
        _payOs = new PayOS(_payOsSetings.ClientId, _payOsSetings.ApiKey, _payOsSetings.ChecksumKey);
    }

    public async Task<ServiceResponse> CreatePaymentLinkByAmountAsync(
    CreatePaymentByAmountRequest requestBody,
    CancellationToken ct = default)
{
    try
    {
        if (requestBody.Amount <= 0)
        {
            return new ServiceResponse
            {
                Succeeded = false,
                Message = "Số tiền phải lớn hơn 0."
            };
        }

        var orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var items = new List<ItemData>
        {
            new ItemData("Thanh toán", 1, requestBody.Amount)
        };

        var expiredAt = TimeConverter
            .GetCurrentVietNamTime()
            .AddSeconds(_payOsSetings.ExpirationSeconds)
            .ToUnixTimeSeconds();

        var data = new PaymentData(
            orderCode: orderCode,
            amount: requestBody.Amount,
            description: $"Thanh toán {requestBody.Amount} VND",
            items: items,
            returnUrl: _payOsSetings.ReturnUrl + $"?orderCode={orderCode}",
            cancelUrl: _payOsSetings.CancelUrl + $"?orderCode={orderCode}",
            expiredAt: expiredAt
        );

        var response = await _payOs.createPaymentLink(data);

        _logger.LogInformation("PayOS link created. orderCode={OrderCode}, amount={Amount}",
            orderCode, requestBody.Amount);

        // ✅ Lưu Payment domain (KHÔNG dùng Transaction)
        var paymentRepo = _unitOfWork.GetRepository<Payment, Guid>();

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            
            OrderId = Guid.Empty,

            PaymentType = PaymentType.PayOs,
            Amount = requestBody.Amount,
            Status = PaymentStatus.Pending,

            QrCodeUrl = response.checkoutUrl,

            TransactionCode = orderCode.ToString(),

            CreatedAt = DateTime.UtcNow,
            VerifiedAt = null,

            UserId = null
        };

        await paymentRepo.AddAsync(payment, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var result = new CreatePaymentByAmountResult
        {
            OrderCode = orderCode,
            Amount = requestBody.Amount,
            PaymentLinkId = response.paymentLinkId,
            CheckoutUrl = response.checkoutUrl,
            QrCode = response.qrCode,
            ExpiredAt = expiredAt
        };

        return new ResponseData<CreatePaymentByAmountResult>
        {
            Succeeded = true,
            Message = "Tạo liên kết thanh toán thành công!",
            Data = result
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating PayOS payment link by amount");
        return new ServiceResponse
        {
            Succeeded = false,
            Message = "Lỗi khi tạo liên kết thanh toán!"
        };
    }
}

    
     public async Task ProcessPayOsWebhookToPaymentAsync(WebhookData data, CancellationToken ct = default)
    {
        var orderCodeStr = data.orderCode.ToString();

        var paymentRepo = _unitOfWork.GetRepository<Payment, Guid>();

        var payment = await paymentRepo.Query()
            .FirstOrDefaultAsync(p => p.TransactionCode == orderCodeStr, ct);

        if (payment is null)
        {
            _logger.LogWarning("PayOS webhook: payment not found. orderCode={OrderCode}", data.orderCode);
            return; // idempotent
        }

        if (payment.Status == PaymentStatus.Success)
        {
            _logger.LogInformation("PayOS webhook: payment already paid. paymentId={PaymentId}, orderCode={OrderCode}",
                payment.Id, data.orderCode);
            return;
        }

        var status = (data.desc?? "").Trim().ToUpperInvariant();

        
        if (status == "PAID" || status == "SUCCESS")
        {
            payment.Status = PaymentStatus.Success;
            payment.VerifiedAt = DateTime.UtcNow;
        }
        else if (status == "CANCEL" || status == "CANCELED" || status == "CANCELLED")
        {
            payment.Status = PaymentStatus.Cancelled;
            payment.VerifiedAt = DateTime.UtcNow;
        }
        else if (status == "EXPIRED")
        {
            payment.Status = PaymentStatus.Failed; 
            payment.VerifiedAt = DateTime.UtcNow;
        }
        else
        {
            payment.Status = PaymentStatus.Failed;
            payment.VerifiedAt = DateTime.UtcNow;

            _logger.LogWarning("PayOS webhook: unknown status='{Status}'. orderCode={OrderCode}", status, data.orderCode);
        }

        await paymentRepo.UpdateAsync(payment, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "PayOS webhook processed. paymentId={PaymentId}, orderCode={OrderCode}, status={Status}",
            payment.Id, data.orderCode, status);
    }
     
     public async Task<ServiceResponse> CreatePaymentLinkByOrderAsync(PaymentRequest requestBody, CancellationToken ct = default)
    {
        try
        {
            // 1) Load order
            var orderRepo = _unitOfWork.GetRepository<Order, Guid>();
            var order = await orderRepo.Query()
                .Include(o => o.Payment) // nếu bạn có navigation 1-1
                .FirstOrDefaultAsync(o => o.Id == requestBody.OrderId, ct);

            if (order == null)
            {
                return new ServiceResponse { Succeeded = false, Message = "Không tìm thấy đơn hàng!" };
            }

            // 2) Lấy amount từ Order
            // PayOS amount cần int (VND). Nếu TotalAmount là decimal, phải convert an toàn.
            if (order.TotalAmount <= 0)
            {
                return new ServiceResponse { Succeeded = false, Message = "Tổng tiền đơn hàng không hợp lệ!" };
            }

            if (order.TotalAmount != decimal.Truncate(order.TotalAmount))
            {
                return new ServiceResponse { Succeeded = false, Message = "Số tiền phải là số nguyên (VND)!" };
            }

            if (order.TotalAmount > int.MaxValue)
            {
                return new ServiceResponse { Succeeded = false, Message = "Số tiền quá lớn!" };
            }

            var amount = checked((int)order.TotalAmount);

            // 3) Nếu đã có payment pending → trả lại link cũ (không tạo mới)
            if (order.Payment != null && order.Payment.Status == PaymentStatus.Pending && !string.IsNullOrWhiteSpace(order.Payment.QrCodeUrl))
            {
                return new ResponseData<object>
                {
                    Succeeded = true,
                    Message = "Đơn hàng đã có liên kết thanh toán (Pending).",
                    Data = new
                    {
                        OrderId = order.Id,
                        Amount = amount,
                        CheckoutUrl = order.Payment.QrCodeUrl,
                        OrderCode = order.Payment.TransactionCode
                    }
                };
            }

            // 4) Tạo orderCode mới cho PayOS
            var orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var items = new List<ItemData>
            {
                new ItemData("Thanh toán đơn hàng", 1, amount)
            };

            var expiredAt = TimeConverter.GetCurrentVietNamTime()
                .AddSeconds(_payOsSetings.ExpirationSeconds)
                .ToUnixTimeSeconds();

            // 5) Return/Cancel trỏ về API của bạn để giả lập thành công/thất bại
            // (bạn yêu cầu dùng orderId)
            var returnUrl = _payOsSetings.ReturnUrl + $"?orderId={order.Id}";
            var cancelUrl = _payOsSetings.CancelUrl + $"?orderId={order.Id}";

            var data = new PaymentData(
                orderCode: orderCode,
                amount: amount,
                description: $"Đã Thanh Toán Đơn Hàng",
                items: items,
                returnUrl: returnUrl,
                cancelUrl: cancelUrl,
                expiredAt: expiredAt
            );

            var response = await _payOs.createPaymentLink(data);

            // 6) Tạo Payment nếu chưa có, hoặc update Payment cũ
            var paymentRepo = _unitOfWork.GetRepository<Payment, Guid>();

            if (order.Payment == null)
            {
                order.Payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    PaymentType = PaymentType.PayOs,
                    Amount = amount,
                    Status = PaymentStatus.Pending,
                    QrCodeUrl = response.checkoutUrl,         // đang dùng field này để lưu checkoutUrl
                    TransactionCode = orderCode.ToString(),   // map orderCode để webhook về sau
                    CreatedAt = DateTime.UtcNow,
                    VerifiedAt = null,
                    UserId = order.UserId
                };

                await paymentRepo.AddAsync(order.Payment, ct);
            }
            else
            {
                // Nếu payment tồn tại nhưng không pending hoặc thiếu link → tạo lại link và set pending
                order.Payment.PaymentType = PaymentType.PayOs;
                order.Payment.Amount = amount;
                order.Payment.Status = PaymentStatus.Pending;
                order.Payment.QrCodeUrl = response.checkoutUrl;
                order.Payment.TransactionCode = orderCode.ToString();
                order.Payment.CreatedAt = DateTime.UtcNow;
                order.Payment.VerifiedAt = null;
                order.Payment.UserId = order.UserId;

                await paymentRepo.UpdateAsync(order.Payment, ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);

            return new ResponseData<object>
            {
                Succeeded = true,
                Message = "Tạo liên kết thanh toán thành công!",
                Data = new
                {
                    OrderId = order.Id,
                    Amount = amount,
                    PaymentLinkId = response.paymentLinkId,
                    CheckoutUrl = response.checkoutUrl,
                    QrCode = response.qrCode,
                    ExpiredAt = expiredAt
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PayOS payment link by order");
            return new ServiceResponse { Succeeded = false, Message = "Lỗi khi tạo liên kết thanh toán!" };
        }
    }

    

}
