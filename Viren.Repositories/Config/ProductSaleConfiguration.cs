using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Repositories.Domains;

namespace Viren.Repositories.Config
{
    public class ProductSaleConfiguration
    : IEntityTypeConfiguration<ProductSale>
    {
        public void Configure(EntityTypeBuilder<ProductSale> b)
        {
            b.ToTable("productSale");

            b.HasKey(x => new { x.ProductId, x.SaleId });
            b.Property(x => x.ProductId)
                   .HasColumnName("productId");

            b.Property(x => x.SaleId)
                   .HasColumnName("saleId");

            b.HasOne(x => x.Product)
                   .WithMany(p => p.ProductSales)
                   .HasForeignKey(x => x.ProductId);

            b.HasOne(x => x.Sale)
                   .WithMany(s => s.ProductSales)
                   .HasForeignKey(x => x.SaleId);
        }
    }

}
