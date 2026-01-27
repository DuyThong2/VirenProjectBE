using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Repositories.Enums;

namespace Viren.Services.Dtos.Requests
{
    public class GetOrderPaginatedRequest
    {
        public Guid UserId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string ? Search { get; set; }
        public string SortBy { get; set; } = "CreatedAt";
        public string SortDirection { get; set; } = "desc";
        public OrderStatus? StatusFilter { get; set; }
    }
}
