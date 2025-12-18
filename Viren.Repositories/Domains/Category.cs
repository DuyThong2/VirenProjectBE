using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Repositories.Common;
using Viren.Repositories.Enums;

namespace Viren.Repositories.Domains
{
    public class Category : BaseEntity<Guid>
    {
        public string Name { get; set; }
        public string? Thumbnail { get; set; }
        public string? Header { get; set; }
        public string? Description { get; set; }
        public CommonStatus Status { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }

}
