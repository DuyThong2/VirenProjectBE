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
    public class UserSubscriptionConfiguration
    : IEntityTypeConfiguration<UserSubscription>
    {
        public void Configure(EntityTypeBuilder<UserSubscription> b)
        {
            b.ToTable("userSubscription");

            b.HasKey(x => x.Id);
            b.Property(x => x.UserId)
                   .IsRequired();

            b.Property(x => x.SubscriptionPlanId)
                   .IsRequired();

            b.Property(x => x.StartDate)
                   .HasColumnName("startDate")
                   .HasColumnType("datetime2")
                   .IsRequired();

            b.Property(x => x.EndDate)
                   .HasColumnName("endDate")
                   .HasColumnType("datetime2")
                   .IsRequired();

            b.Property(x => x.Status)
                   .HasColumnName("status")
                   .HasDefaultValue(CommonStatus.Active)
                   .HasConversion<string>()
                   .HasColumnType("nvarchar(50)");

            b.HasOne(x => x.User)
                   .WithMany(u => u.UserSubscriptions)
                   .HasForeignKey(x => x.UserId);

            b.HasOne(x => x.SubscriptionPlan)
                   .WithMany(sp => sp.UserSubscriptions)
                   .HasForeignKey(x => x.SubscriptionPlanId);
        }
    }

}
