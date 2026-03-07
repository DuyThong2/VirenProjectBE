namespace Viren.Services.Dtos.Response
{
    public class TopCustomerResponse
    {
        public Guid UserId { get; set; }

        public string CustomerName { get; set; } = null!;

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public int TotalOrders { get; set; }

        public decimal TotalSpent { get; set; }

        public DateTime? LastOrderAt { get; set; }
    }
}