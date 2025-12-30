using Net.payOS.Types;
using Viren.Services.ApiResponse;
using Viren.Services.Dtos.Requests;
using Viren.Services.Dtos.Response;

namespace Viren.Services.Interfaces;


    public interface IPaymentService
    {
        Task<ServiceResponse> CreatePaymentLinkByAmountAsync(
            CreatePaymentByAmountRequest requestBody,
            CancellationToken ct = default);
        
        Task ProcessPayOsWebhookToPaymentAsync(WebhookData data, CancellationToken ct = default);
        
        Task<ServiceResponse> CreatePaymentLinkByOrderAsync(PaymentRequest requestBody, CancellationToken ct = default);

        

    }

