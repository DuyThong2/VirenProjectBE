using Microsoft.AspNetCore.Mvc;
using Viren.Repositories.Enums;
using Viren.Services.Interfaces;

namespace Viren.API.Controllers
{
    [ApiController]
    [Route("api/admin/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _orderService;

        public DashboardController(IDashboardService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Lấy dữ liệu tổng quan dashboard
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetDashboardSummary(CancellationToken ct)
        {
            var result = await _orderService.GetDashboardSummaryAsync(ct);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("revenue-chart")]
        public async Task<IActionResult> GetRevenueChart(
            [FromQuery] string period = "day",
            CancellationToken ct = default)
        {
            var result = await _orderService.GetRevenueChartAsync(period, ct);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("order-chart")]
        public async Task<IActionResult> GetOrderChart(
    [FromQuery] string period = "day",
    CancellationToken ct = default)
        {
            var result = await _orderService.GetOrderChartAsync(period, ct);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("top-products")]
        public async Task<IActionResult> GetTopProducts(
            [FromQuery] int limit = 5,
            CancellationToken ct = default)
        {
            var result = await _orderService.GetTopProductsAsync(limit, ct);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("delayed-orders")]
        public async Task<IActionResult> GetDelayedOrders(
            [FromQuery] int limit = 10,
            CancellationToken ct = default)
        {
            var result = await _orderService.GetDelayedOrdersAsync(limit, ct);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("stock-alerts")]
        public async Task<IActionResult> GetStockAlerts(
            [FromQuery] int limit = 10,
            CancellationToken ct = default)
        {
            var result = await _orderService.GetStockAlertsAsync(limit, ct);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("top-customers")]
        public async Task<IActionResult> GetTopCustomers(
            [FromQuery] int limit = 10,
            CancellationToken ct = default)
        {
            var result = await _orderService.GetTopCustomersAsync(limit, ct);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }
    }
}