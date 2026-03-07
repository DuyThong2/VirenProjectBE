namespace Viren.Services.Dtos.Response
{
    public class OrderChartPoint
    {
        public string Label { get; set; } = null!;

        public int Pending { get; set; }
        public int Paid { get; set; }
        public int Shipping { get; set; }
        public int Completed { get; set; }
        public int Cancelled { get; set; }
    }

    public class OrderChartResponse
    {
        public string Period { get; set; } = null!;
        public List<OrderChartPoint> Points { get; set; } = new();
    }
}