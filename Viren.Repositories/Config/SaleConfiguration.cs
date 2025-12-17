using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Repositories.Domains;
using Viren.Repositories.Enums;

namespace Viren.Repositories.Config
{
    public class SaleConfiguration
    : IEntityTypeConfiguration<Sale>
    {
        public void Configure(EntityTypeBuilder<Sale> b)
        {
            b.ToTable("sale");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id)
                   .HasColumnName("saleId");
                
            b.Property(x => x.Name)
                   .HasColumnName("name")
                   .HasColumnType("nvarchar(150)")
                   .IsRequired();

            b.Property(x => x.DiscountType)
                   .HasColumnName("discountType")
                   .HasDefaultValue(DiscountType.Percentage)
                   .HasConversion<string>()
                   .HasColumnType("nvarchar(50)")
                   .IsRequired();

            b.Property(x => x.DiscountValue)
                   .HasColumnName("discountValue")
                   .HasPrecision(18, 2);

            b.Property(x => x.Status)
                   .HasColumnName("status")
                   .HasDefaultValue(CommonStatus.Active)
                   .HasConversion<string>()
                   .HasColumnType("nvarchar(50)");

            b.Property(x => x.StartDate)
                   .HasColumnType("datetime2");

            b.Property(x => x.EndDate)
                   .HasColumnType("datetime2");

            b.HasMany(x => x.ProductSales)
                   .WithOne(ps => ps.Sale)
                   .HasForeignKey(ps => ps.SaleId);

        }
    }

}
