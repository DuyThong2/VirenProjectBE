using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Repositories.Enums;

namespace Viren.Services.Dtos.Requests
{
    public class ProductRequestDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? Detail { get; set; }
        public string? Care { get; set; }
        public string? Commitment { get; set; }
        public string? Thumbnail { get; set; }
        public decimal Price { get; set; }
        public CommonStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Guid CategoryId { get; set; }
    }
}
