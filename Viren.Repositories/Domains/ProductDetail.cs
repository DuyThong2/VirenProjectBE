using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Repositories.Common;
using Viren.Repositories.Enums;

namespace Viren.Repositories.Domains
{
    public class ProductDetail
    {
        public Guid Id { get; set; }

        public Guid ProductId { get; set; }
        public string Size { get; set; } = null!;
        public string Color { get; set; } = null!;
        public int Stock { get; set; }
        public string? Images { get; set; }
        public CommonStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Product Product { get; set; } = null!;
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
    }

}
