using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Repositories.Common;
using Viren.Repositories.Enums;

namespace Viren.Repositories.Domains
{
    public class SubscriptionPlan
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public CommonStatus Status { get; set; }

        public ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
    }
}
