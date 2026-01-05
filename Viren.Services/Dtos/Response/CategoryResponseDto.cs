using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viren.Services.Dtos.Response
{
    public class CategoryResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Thumbnail { get; set; }
        public string? Header { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = null!;
        public List<ProductResponseDto> Products { get; set; } = new List<ProductResponseDto>();
    }
}
