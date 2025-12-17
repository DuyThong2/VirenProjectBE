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
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> b)
        {
            b.ToTable("product");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id)
                .HasColumnName("productId");

            b.Property(x => x.CategoryId)
                .HasColumnName("categoryId");

            b.Property(x => x.Name)
                .HasColumnName("name")
                .HasColumnType("nvarchar(200)")
                .IsRequired();

            b.Property(x => x.Description)
                .HasColumnName("description")
                .HasColumnType("nvarchar(max)");

            b.Property(x => x.Detail)
                .HasColumnName("detail")
                .HasColumnType("nvarchar(max)");

            b.Property(x => x.Care)
                .HasColumnName("care")
                .HasColumnType("nvarchar(max)");

            b.Property(x => x.Commitment)
                .HasColumnName("commitment")
                .HasColumnType("nvarchar(max)");

            b.Property(x => x.Thumbnail)
                .HasColumnName("thumbnail")
                .HasColumnType("nvarchar(500)");

            b.Property(x => x.Price)
                .HasColumnName("price")
                .HasPrecision(18, 2);

            b.Property(x => x.Status)
                .HasColumnName("status")
                .HasDefaultValue(CommonStatus.Active)
                .HasConversion<string>()
                .HasColumnType("nvarchar(50)");

            b.Property(x => x.CreatedAt)
                .HasColumnName("created_at");

            b.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at");

            // Relationships
            //b.HasOne(x => x.Category)
            //    .WithMany(x => x.Products)
            //    .HasForeignKey(x => x.CategoryId)
            //    .OnDelete(DeleteBehavior.Restrict);

            b.HasMany(x => x.ProductDetails)
                .WithOne(x => x.Product)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(x => x.ProductSales)
                .WithOne(x => x.Product)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }


}
