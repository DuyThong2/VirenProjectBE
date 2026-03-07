namespace Viren.Services.Dtos.Response
{
    public class DelayedOrderResponse
    {
        public Guid OrderId { get; set; }

        public string OrderCode { get; set; } = null!;

        public string CustomerName { get; set; } = null!;

        public decimal TotalAmount { get; set; }

        public string OrderStatus { get; set; } = null!;

        public string PaymentStatus { get; set; } = null!;

        public string DelayedType { get; set; } = null!;

        public int DelayedHours { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? PaidAt { get; set; }
    }
}