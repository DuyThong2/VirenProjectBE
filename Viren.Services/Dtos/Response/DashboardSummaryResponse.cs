namespace Viren.Services.Dtos.Response
{
    public class DashboardSummaryResponse
    {
        public decimal RevenueToday { get; set; }
        public decimal RevenueThisWeek { get; set; }
        public decimal RevenueThisMonth { get; set; }

        public int OrdersToday { get; set; }
        public int OrdersThisWeek { get; set; }
        public int OrdersThisMonth { get; set; }

        public int TotalCustomers { get; set; }
        public int TotalProducts { get; set; }

        public int PendingOrdersCount { get; set; }
        public int PaidButUnprocessedOrdersCount { get; set; }

        public int OutOfStockCount { get; set; }
        public int LowStockCount { get; set; }
    }
}