using Microsoft.AspNetCore.Mvc;
using Viren.Services.Dtos.Requests;
using Viren.Services.Interfaces;

namespace Viren.API.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    // POST /api/payments/payos/by-amount
    // body: { "amount": 2000 }
    [HttpPost("payos/by-amount")]
    public async Task<IActionResult> CreateByAmount([FromBody] CreatePaymentByAmountRequest req, CancellationToken ct)
    {
        var res = await _paymentService.CreatePaymentLinkByAmountAsync(req, ct);
        return res.Succeeded ? Ok(res) : BadRequest(res);
    }

    // Đây là endpoint để PayOS redirect user về
    [HttpGet("return")]
    public IActionResult Return([FromQuery] long orderCode)
        => Ok(new { message = "return", orderCode });

    [HttpGet("cancel")]
    public IActionResult Cancel([FromQuery] long orderCode)
        => Ok(new { message = "cancel", orderCode });
}