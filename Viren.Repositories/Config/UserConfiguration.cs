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
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("user");

            builder.Property(x => x.Name)
                   .HasColumnName("name")
                   .HasColumnType("nvarchar")
                   .HasMaxLength(150)
                   .IsRequired();

            builder.Property(x => x.Gender)
                   .HasColumnName("gender");
                   

            builder.Property(x => x.Birthdate)
                   .HasColumnName("birthdate");

            builder.Property(x => x.Height)
                   .HasColumnName("height")
                   .HasPrecision(5, 2);

            builder.Property(x => x.Weight)
                   .HasColumnName("weight")
                   .HasPrecision(5, 2);

            builder.Property(x => x.Status)
                   .HasColumnName("status")
                   .HasDefaultValue(CommonStatus.Active)
                   .HasConversion<string>()
                   .HasColumnType("nvarchar(50)");

            builder.Property(x => x.CreatedAt)
                   .HasColumnName("createdAt")
                   .HasDefaultValueSql("CURRENT_TIMESTAMP");
           
        }
    }

}
