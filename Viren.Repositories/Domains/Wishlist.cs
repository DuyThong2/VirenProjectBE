using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Repositories.Common;

namespace Viren.Repositories.Domains
{
    public class Wishlist
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public Guid ProductDetailId { get; set; }
        public DateTime CreatedAt { get; set; }

        public User User { get; set; } = null!;
        public ProductDetail ProductDetail { get; set; } = null!;
    }

}
