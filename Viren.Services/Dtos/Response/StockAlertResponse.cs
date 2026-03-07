namespace Viren.Services.Dtos.Response
{
    public class StockAlertResponse
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; } = null!;

        public string? ThumbnailUrl { get; set; }

        public string? Sku { get; set; }

        public int StockQuantity { get; set; }

        public int Threshold { get; set; }

        public string Status { get; set; } = null!;
    }
}