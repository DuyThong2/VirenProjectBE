using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viren.Services.Dtos.Response
{
    public class ProductDetailResponse
    {
        public Guid Id { get; set; }
        public string Size { get; set; } = null!;
        public string Color { get; set; } = null!;
        public int Stock { get; set; }
        public string? Images { get; set; }
    }
}
