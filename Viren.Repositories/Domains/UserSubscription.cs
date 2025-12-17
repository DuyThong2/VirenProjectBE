using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Repositories.Common;
using Viren.Repositories.Enums;

namespace Viren.Repositories.Domains
{
    public class UserSubscription
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public Guid SubscriptionPlanId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public CommonStatus Status { get; set; }

        // Navigation
        public User User { get; set; } = null!;
        public SubscriptionPlan SubscriptionPlan { get; set; } = null!;
    }
}
