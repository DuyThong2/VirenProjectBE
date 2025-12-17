using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viren.Repositories.Domains
{
    public class ProductSale
    {
        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public Guid SaleId { get; set; }
        public Sale Sale { get; set; } = null!;
    }

}
