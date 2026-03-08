namespace Viren.Services.Dtos.Response
{
    public class RevenueChartPoint
    {
        public string Label { get; set; } = null!;
        public decimal Revenue { get; set; }
    }

    public class RevenueChartResponse
    {
        public string Period { get; set; } = null!;
        public List<RevenueChartPoint> Points { get; set; } = new();
    }
}