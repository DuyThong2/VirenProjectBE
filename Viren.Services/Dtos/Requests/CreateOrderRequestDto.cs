using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viren.Services.Dtos.Requests
{
    public class CreateOrderRequestDto
    {
        public Guid UserId { get; set; }
        public string ShippingAddress { get; set; } = null!;
        public string? Note { get; set; }
        public List<CreateOrderItemDto> Items { get; set; } = new();
    }

    public class CreateOrderItemDto
    {
        public Guid ProductDetailId { get; set; }
        public int Quantity { get; set; }
    }
}
