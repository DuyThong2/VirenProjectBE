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
    public class WishlistConfiguration
    : IEntityTypeConfiguration<Wishlist>
    {
        public void Configure(EntityTypeBuilder<Wishlist> b)
        {
            b.ToTable("wishlist");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id)
                   .HasColumnName("wishlistId");

            b.Property(x => x.UserId);

            b.Property(x => x.ProductDetailId);

            b.Property(x => x.CreatedAt)
                   .HasColumnType("datetime2")
                   .HasDefaultValueSql("SYSDATETIME()");

            b.HasOne(x => x.User)
                   .WithMany(u => u.Wishlists)
                   .HasForeignKey(x => x.UserId);

            b.HasOne(x => x.ProductDetail)
                   .WithMany(pd => pd.Wishlists)
                   .HasForeignKey(x => x.ProductDetailId);
        }
    }

}
