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
    public class SubscriptionPlanConfiguration
    : IEntityTypeConfiguration<SubscriptionPlan>
    {
        public void Configure(EntityTypeBuilder<SubscriptionPlan> b)
        {
            b.ToTable("subscriptionPlan");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id)
                   .HasColumnName("subscriptionPlanId");

            b.Property(x => x.Name)
                   .HasColumnName("name")
                   .HasColumnType("nvarchar(150)")
                   .IsRequired();

            b.Property(x => x.Price)
                   .HasColumnName("price")
                   .HasPrecision(18, 2);

            b.Property(x => x.DurationDays)
                   .HasColumnName("durationDays")
                   .IsRequired();

            b.HasMany(x => x.UserSubscriptions)
                   .WithOne(x => x.SubscriptionPlan)
                   .HasForeignKey(x => x.SubscriptionPlanId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

}
