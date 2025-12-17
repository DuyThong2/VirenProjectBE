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
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> b)
        {
            b.ToTable("order");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id)
                .HasColumnName("orderId");

            b.Property(x => x.UserId)
                .HasColumnName("userId");

            b.Property(x => x.TotalAmount)
                .HasColumnName("totalAmount")
                .HasPrecision(18, 2);

            b.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasDefaultValue(OrderStatus.Pending)
                .HasColumnType("nvarchar(50)");

            b.Property(x => x.ShippingAddress)
                .HasColumnName("shippingAddress")
                .HasColumnType("nvarchar(max)");

            b.Property(x => x.Note)
                .HasColumnName("note")
                .HasColumnType("nvarchar(max)");

            b.Property(x => x.CreatedAt)
                .HasColumnName("createdAt");

            // Relationships
            b.HasOne(x => x.User)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasMany(x => x.OrderItems)
                .WithOne(x => x.Order)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(x => x.Payments)
                .WithOne(x => x.Order)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

        }
    }

}
