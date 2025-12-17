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
    public class ProductDetailConfiguration : IEntityTypeConfiguration<ProductDetail>
    {
        public void Configure(EntityTypeBuilder<ProductDetail> b)
        {
            b.ToTable("productDetail");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id)
                .HasColumnName("productDetailId");

            b.Property(x => x.ProductId)
                .HasColumnName("productId");

            b.Property(x => x.Size)
                .HasColumnName("size")
                .HasColumnType("nvarchar(50)");

            b.Property(x => x.Color)
                .HasColumnName("color")
                .HasColumnType("nvarchar(50)");

            b.Property(x => x.Stock)
                .HasColumnName("stock");

            b.Property(x => x.Images)
                .HasColumnName("images")
                .HasColumnType("nvarchar(max)");

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
            b.HasOne(x => x.Product)
                .WithMany(x => x.ProductDetails)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(x => x.OrderItems)
                .WithOne(x => x.ProductDetail)
                .HasForeignKey(x => x.ProductDetailId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasMany(x => x.Wishlists)
                .WithOne(x => x.ProductDetail)
                .HasForeignKey(x => x.ProductDetailId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

}
