using Microsoft.AspNetCore.Mvc;
using Viren.Services.Dtos.Requests;
using Viren.Services.Interfaces;

namespace Viren.API.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IOrderService _orderService;

    public PaymentsController(IPaymentService paymentService, IOrderService orderService)
    {
        _paymentService = paymentService;
        _orderService = orderService;
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
    [HttpPost("payos/by-order")]
    public async Task<IResult> CreateByOrder([FromBody] PaymentRequest req, CancellationToken ct)
    {
        var res = await _paymentService.CreatePaymentLinkByOrderAsync(req, ct);
        return res.Succeeded ? TypedResults.Ok(res) : TypedResults.BadRequest(res);
    }

    [HttpGet("return")]
    public async Task<IResult> PaymentReturn(
        [FromQuery] Guid orderId,
        CancellationToken ct = default)
    {
        var res = await _orderService.MarkOrderPaidAsync(orderId, ct);
        return res.Succeeded
            ? TypedResults.Ok(res)
            : TypedResults.BadRequest(res);
    }

    [HttpGet("cancel")]
    public async Task<IResult> PaymentCancel(
        [FromQuery] Guid orderId,
        CancellationToken ct = default)
    {
        var res = await _orderService.MarkOrderCancelledAsync(orderId, ct);
        return res.Succeeded
            ? TypedResults.Ok(res)
            : TypedResults.BadRequest(res);
    }

    [HttpGet]
    public async Task<IActionResult> GetPayments([FromQuery] GetPaymentsRequest req, CancellationToken ct)
    {
        var res = await _paymentService.GetPaymentsAsync(req, ct);
        return Ok(res);
    }

    [HttpGet("/api/users/{userId:guid}/payments")]
    public async Task<IActionResult> GetUserPayments(
    [FromRoute] Guid userId,
    [FromQuery] GetPaymentsRequest req,
    CancellationToken ct)
    {
        req.UserId = userId;
        var res = await _paymentService.GetPaymentsAsync(req, ct);
        return Ok(res);
    }

}