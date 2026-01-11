using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Repositories.Enums;

namespace Viren.Services.Dtos.Requests
{
    public sealed class GetPaymentsRequest
    {
        public Guid? UserId { get; set; }            

        public Guid? OrderId { get; set; }          
        public string? TransactionCode { get; set; } 

        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }

        public PaymentType? PaymentType { get; set; }
        public PaymentStatus? Status { get; set; }

        public DateTime? FromDate { get; set; }      
        public DateTime? ToDate { get; set; }        

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }



}
