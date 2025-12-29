namespace Viren.Services.Dtos.Response;

public sealed class CreatePaymentByAmountResult
{
    public long OrderCode { get; set; }
    public int Amount { get; set; }
    public string PaymentLinkId { get; set; } = null!;
    public string CheckoutUrl { get; set; } = null!;
    public string QrCode { get; set; } = null!;
    public long ExpiredAt { get; set; }
}