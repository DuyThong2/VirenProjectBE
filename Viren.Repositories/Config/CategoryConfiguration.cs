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
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> b)
        {
            b.ToTable("category");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id)
                .HasColumnName("categoryId");

            b.Property(x => x.Name)
                .HasColumnName("name")
                .HasColumnType("nvarchar")
                .HasMaxLength(150)
                .IsRequired();

            b.Property(x => x.Thumbnail)
                .HasColumnName("thumbnail")
                .HasColumnType("nvarchar")
                .HasMaxLength(500);

            b.Property(x => x.Header)
                .HasColumnName("header")
                .HasColumnType("nvarchar")
                .HasMaxLength(255);

            b.Property(x => x.Description)
                .HasColumnName("description")
                .HasColumnType("nvarchar(max)");

            b.Property(x => x.Status)
                .HasColumnName("status")
                .HasDefaultValue(CommonStatus.Active)
                .HasConversion<string>()
                .HasColumnType("nvarchar(50)");
                

            // Relationships
            b.HasMany(x => x.Products)
                .WithOne(x => x.Category)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }


}
