using Microsoft.AspNetCore.Mvc;
using Net.payOS;
using Net.payOS.Types;
using Viren.Services.Interfaces;

namespace Viren.API.Controllers;

[ApiController]
[Route("api/payments/payos")]
public sealed class PayOsWebhookController : ControllerBase
{
    private readonly PayOS _payOS;
    private readonly ILogger<PayOsWebhookController> _logger;
    private readonly IPaymentService _paymentService;

    public PayOsWebhookController(PayOS payOS, ILogger<PayOsWebhookController> logger,  IPaymentService paymentService)
    {
        _payOS = payOS;
        _logger = logger;
        _paymentService = paymentService;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] WebhookType body, CancellationToken ct)
    {
        try
        {
            WebhookData verifiedData = _payOS.verifyPaymentWebhookData(body);
            await _paymentService.ProcessPayOsWebhookToPaymentAsync(verifiedData, ct);
            return Ok("OK");
        }
        catch
        {
            return BadRequest("Invalid webhook");
        }
    }
}