using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Repositories.Enums;

namespace Viren.Services.Dtos.Response
{
    public sealed class PaymentDtoResponse
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }

        public PaymentType PaymentType { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }

        public string? TransactionCode { get; set; }
        public string? QrCodeUrl { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }

        public Guid? UserId { get; set; }
        public string? FullName { get; set; }    
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
