using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Repositories.Domains;
using Viren.Repositories.Enums;

namespace Viren.Services.Dtos.Requests
{
    public class ProductDetailRequestDto
    {
        public string Size { get; set; } = null!;
        public string Color { get; set; } = null!;
        public int Stock { get; set; }
        public string? Images { get; set; }
        public CommonStatus Status { get; set; }


        public Guid ProductId { get; set; }
    }
}
