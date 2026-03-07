namespace Viren.Services.Dtos.Response
{
    public class TopProductResponse
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; } = null!;

        public string? ThumbnailUrl { get; set; }

        public int SoldQuantity { get; set; }

        public decimal Revenue { get; set; }

        public int StockQuantity { get; set; }
    }
}