using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.payOS;
using Net.payOS.Types;
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

    public PaymentService(
        ILogger<PaymentService> logger,
        IOptions<PayOsSetings> payOsSetings)
    {
        _logger = logger;
        _payOsSetings = payOsSetings.Value;

        _payOs = new PayOS(_payOsSetings.ClientId, _payOsSetings.ApiKey, _payOsSetings.ChecksumKey);
    }

    public async Task<ServiceResponse> CreatePaymentLinkByAmountAsync(
        CreatePaymentByAmountRequest requestBody,
        CancellationToken ct = default)
    {
        
        
        try
        {
            // validate
            if (requestBody.Amount <= 0)
            {
                return new ServiceResponse
                {
                    Succeeded = false,
                    Message = "Số tiền phải lớn hơn 0."
                };
            }

            // PayOS amount là int (VND). Bạn đã để int rồi nên OK.

            var orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // item tối giản
            var item = new ItemData("Thanh toán", 1, requestBody.Amount);
            var items = new List<ItemData> { item };

            // expiredAt
            var expiredAt = TimeConverter
                .GetCurrentVietNamTime()
                .AddSeconds(_payOsSetings.ExpirationSeconds)
                .ToUnixTimeSeconds();

            var baseUrl = _payOsSetings.BaseUrl.TrimEnd('/');

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

            _logger.LogInformation("PayOS link created. orderCode={OrderCode}, amount={Amount}", orderCode, requestBody.Amount);

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
}
