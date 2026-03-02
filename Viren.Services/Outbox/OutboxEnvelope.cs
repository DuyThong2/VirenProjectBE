using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viren.Services.Outbox
{
    public sealed class OutboxEnvelope
    {
        public Guid OutboxEventId { get; init; }
        public string EventType { get; init; } = null!;
        public string AggregateType { get; init; } = null!;
        public Guid AggregateId { get; init; }
        public int SchemaVersion { get; init; }
        public string Payload { get; init; } = null!;
        public Guid? CorrelationId { get; init; }
        public string? PartitionKey { get; init; }
        public DateTime CreatedAtUtc { get; init; }
    }
}
