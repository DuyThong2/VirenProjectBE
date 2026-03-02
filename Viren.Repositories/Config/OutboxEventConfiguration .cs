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
    public class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
    {
        public void Configure(EntityTypeBuilder<OutboxEvent> builder)
        {
            builder.ToTable("OutboxEvents");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.AggregateType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.EventType)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Payload)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            builder.Property(x => x.SchemaVersion)
                .HasDefaultValue(1);

            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<int>(); // enum -> int

            builder.Property(x => x.RetryCount)
                .HasDefaultValue(0);

            builder.Property(x => x.LastError)
                .HasMaxLength(2000);

            builder.Property(x => x.PartitionKey)
                .HasMaxLength(100);

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            // Indexes for relay worker
            builder.HasIndex(x => new { x.Status, x.NextRetryAt, x.CreatedAt })
                .HasDatabaseName("IX_OutboxEvents_Status_NextRetryAt");

            builder.HasIndex(x => new { x.AggregateType, x.AggregateId })
                .HasDatabaseName("IX_OutboxEvents_Aggregate");

            builder.HasIndex(x => x.CorrelationId)
                .HasDatabaseName("IX_OutboxEvents_CorrelationId");
        }
    }
}
