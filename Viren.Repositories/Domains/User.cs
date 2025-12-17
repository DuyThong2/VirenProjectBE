using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Repositories.Common;

namespace Viren.Repositories.Domains
{
    using Microsoft.AspNetCore.Identity;
    using Viren.Repositories.Enums;

    public class User : IdentityUser<Guid>
    {
        public string Name { get; set; } = null!;
        public bool Gender { get; set; }
        public DateTime? Birthdate { get; set; }
        public decimal? Height { get; set; }
        public decimal? Weight { get; set; }
        public CommonStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
        public ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
    }

}
