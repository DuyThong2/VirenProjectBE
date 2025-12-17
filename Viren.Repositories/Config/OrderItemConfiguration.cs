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
    public class OrderItemConfiguration
        : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> b)
        {
            // Table
            b.ToTable("order_items");

            // Primary Key
            b.HasKey(x => x.Id);

            // Columns
            b.Property(x => x.Id)
             .HasColumnName("orderItemId");

            b.Property(x => x.OrderId)
             .IsRequired();

            b.Property(x => x.ProductDetailId)
             .IsRequired();

            b.Property(x => x.Quantity)
             .IsRequired();

            b.Property(x => x.Price)
             .HasPrecision(18, 2)
             .IsRequired();
        }
    }
}
