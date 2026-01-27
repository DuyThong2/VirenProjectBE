using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Repositories.Enums;

namespace Viren.Services.Dtos.Response
{
    public class OrderResponseDto
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public string ShippingAddress { get; set; } = null!;
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public OrderStatus Status { get; set; }
        public List<OrderItemResponseDto> Items { get; set; } = new();
    }

    public class OrderItemResponseDto
    {
        public Guid ProductDetailId { get; set; }
        public string ProductName { get; set; } = null!;
        public string? Images { get; set; }
        public string Size { get; set; } = null!;
        public string Color { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
