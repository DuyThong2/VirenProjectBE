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
        public PaymentType PaymentType { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }
        public string? QrCodeUrl { get; set; }
        public string? TransactionCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
        
        public User?  User { get; set; }
        public Guid? UserId { get; set; }
        public Order Order { get; set; } = null!;
        
        
    }

}
