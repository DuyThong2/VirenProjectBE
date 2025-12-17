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
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> b)
        {
            b.ToTable("payment");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id)
                .HasColumnName("paymentId");

            b.Property(x => x.OrderId)
                .HasColumnName("orderId");

            b.Property(x => x.PaymentType)
                .HasColumnName("paymentType")
                .HasColumnType("nvarchar(50)");

            b.Property(x => x.Amount)
                .HasColumnName("amount")
                .HasPrecision(18, 2);

            b.Property(x => x.Status)
                .HasColumnName("status")
                .HasDefaultValue(PaymentStatus.Pending)
                .HasConversion<string>()
                .HasColumnType("nvarchar(50)");

            b.Property(x => x.QrCodeUrl)
                .HasColumnName("qrCodeUrl")
                .HasColumnType("nvarchar(500)");

            b.Property(x => x.TransactionCode)
                .HasColumnName("transactionCode")
                .HasColumnType("nvarchar(100)");

            b.Property(x => x.CreatedAt)
                .HasColumnName("created_at");

            b.Property(x => x.VerifiedAt)
                .HasColumnName("verified_at");

            // Relationships
            b.HasOne(x => x.Order)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

}
