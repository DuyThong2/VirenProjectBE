using Microsoft.AspNetCore.Mvc;
using Viren.Repositories.Enums;
using Viren.Services.Dtos.Requests;
using Viren.Services.Interfaces;

namespace Viren.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }
        [HttpPost]
        public async Task<IActionResult> CreateOrderAsync(
            [FromBody] OrderRequestDto request,
            CancellationToken cancellationToken)
        {
            var serviceResponse = await _orderService.CreateOrderAsync(request, cancellationToken);

            if (!serviceResponse.Succeeded)
            {
                return BadRequest(serviceResponse);
            }

            return Ok(serviceResponse);
        }

        [HttpGet]
        public async Task<IActionResult> GetOrdersAsync(
            [FromQuery] Guid? userId,
            [FromQuery] int? statusFilter,
            [FromQuery] string? search,
            [FromQuery] string? sortBy = "createdat",
            [FromQuery] string? sortDirection = "desc",  
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;
            var request = new GetOrderPaginatedRequest
            {
                UserId = userId ?? Guid.Empty,
                Page = page,
                PageSize = pageSize,
                Search = search,
                SortBy = sortBy ?? "CreatedAt",
                StatusFilter = statusFilter.HasValue? (OrderStatus?)statusFilter.Value : null,
                SortDirection = sortDirection ?? "desc",
            };
            var serviceResponse = await _orderService.GetOrdersAsync(request, cancellationToken);
            if (!serviceResponse.Succeeded)
            {
                return BadRequest(serviceResponse);
            }
            return Ok(serviceResponse);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderByIdAsync(
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
        {
            var serviceResponse = await _orderService.GetOrderByIdAsync(id, cancellationToken);
            if (!serviceResponse.Succeeded)
            {
                return BadRequest(serviceResponse);
            }
            return Ok(serviceResponse);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrderAsync(
            [FromRoute] Guid id,
            [FromBody] OrderRequestDto request,
            CancellationToken cancellationToken)
        {
            var serviceResponse = await _orderService.UpdateOrderAsync(id, request, cancellationToken);
            if (!serviceResponse.Succeeded)
            {
                return BadRequest(serviceResponse);
            }
            return Ok(serviceResponse);
        }

        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelOrderAsync(
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
        {
            var serviceResponse = await _orderService.MarkOrderCancelledAsync(id, cancellationToken);
            if (!serviceResponse.Succeeded)
            {
                return BadRequest(serviceResponse);
            }
            return Ok(serviceResponse);
        }
    }
}
