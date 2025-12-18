using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Repositories.Common;
using Viren.Repositories.Enums;

namespace Viren.Repositories.Domains
{
    public class Payment : BaseEntity<Guid>
    {
        public Guid OrderId { get; set; }
        public string PaymentType { get; set; } = null!;
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }
        public string? QrCodeUrl { get; set; }
        public string? TransactionCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }

        public Order Order { get; set; } = null!;
    }

}
