using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Services.ApiResponse;
using Viren.Services.Dtos.Response;

namespace Viren.Services.Interfaces
{
    public interface IDashboardService
    {


        Task<ResponseData<DashboardSummaryResponse>> GetDashboardSummaryAsync(CancellationToken ct);

        Task<ResponseData<RevenueChartResponse>> GetRevenueChartAsync(string period, CancellationToken ct);

        Task<ResponseData<OrderChartResponse>> GetOrderChartAsync(
            string period,

            CancellationToken ct);
        Task<ResponseData<List<TopProductResponse>>> GetTopProductsAsync(int limit, CancellationToken ct);

        Task<ResponseData<List<DelayedOrderResponse>>> GetDelayedOrdersAsync(int limit, CancellationToken ct);

        Task<ResponseData<List<StockAlertResponse>>> GetStockAlertsAsync(int limit, CancellationToken ct);

        Task<ResponseData<List<TopCustomerResponse>>> GetTopCustomersAsync(int limit, CancellationToken ct);
    }
}
