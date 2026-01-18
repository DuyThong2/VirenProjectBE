using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Viren.Repositories.Common;
using Viren.Repositories.Enums;

namespace Viren.Repositories.Domains
{
    public class OutboxEvent : BaseEntity<Guid>
    {
        public string AggregateType { get; set; } = null!;
        public Guid AggregateId { get; set; }

        public string EventType { get; set; } = null!;

        public string Payload { get; set; } = null!;
        public int SchemaVersion { get; set; } = 1;

        public OutboxStatus Status { get; set; } = OutboxStatus.Pending;
        public int RetryCount { get; set; } = 0;
        public DateTime? NextRetryAt { get; set; }
        public string? LastError { get; set; }

        public Guid? CorrelationId { get; set; }
        public string? PartitionKey { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PublishedAt { get; set; }

        // helper (optional)
        public T? DeserializePayload<T>() =>
            JsonSerializer.Deserialize<T>(Payload);
    }
}
