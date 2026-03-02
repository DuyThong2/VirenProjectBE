using System.Text.Json;
using Viren.Repositories.Domains;
using Viren.Repositories.Enums;

namespace Viren.Services.Outbox
{
    public static class OutboxFactory
    {
        private static readonly JsonSerializerOptions _jsonOpts = new(JsonSerializerDefaults.Web);

        public static OutboxEvent Create<TPayload>(
            string aggregateType,
            Guid aggregateId,
            string eventType,
            TPayload payload,
            Guid? correlationId = null,
            string? partitionKey = null,
            int schemaVersion = 1)
        {
            return new OutboxEvent
            {
                Id = Guid.NewGuid(),
                AggregateType = aggregateType,
                AggregateId = aggregateId,
                EventType = eventType,
                Payload = JsonSerializer.Serialize(payload, _jsonOpts),
                SchemaVersion = schemaVersion,
                Status = OutboxStatus.Pending,
                RetryCount = 0,
                NextRetryAt = null,
                LastError = null,
                CorrelationId = correlationId,
                PartitionKey = partitionKey,
                CreatedAt = DateTime.UtcNow,
                PublishedAt = null
            };
        }
    }
}
