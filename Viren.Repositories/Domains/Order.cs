using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Repositories.Common;
using Viren.Repositories.Enums;

namespace Viren.Repositories.Domains
{
    public class Order : BaseEntity<Guid>
    {

        public Guid UserId { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }
        public string ShippingAddress { get; set; } = null!;
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }

        public User User { get; set; } = null!;
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        /*
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    */
        
        public Payment? Payment { get; set; }

    }

}
