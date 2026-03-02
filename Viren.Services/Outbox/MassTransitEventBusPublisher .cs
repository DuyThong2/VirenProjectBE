using MassTransit;
using Microsoft.Extensions.Options;
using Viren.Repositories.Domains;
using Viren.Services.Configs;
using Viren.Services.IntegrationEvents;

namespace Viren.Services.Outbox
{
    public sealed class MassTransitEventBusPublisher : IEventBusPublisher
    {
        private readonly ISendEndpointProvider _sendEndpointProvider;
        private readonly MessageBrokerSettings _settings;

        public MassTransitEventBusPublisher(
            ISendEndpointProvider sendEndpointProvider,
            IOptions<MessageBrokerSettings> options)
        {
            _sendEndpointProvider = sendEndpointProvider;
            _settings = options.Value;
        }

        public async Task PublishAsync(OutboxEvent evt, CancellationToken ct)
        {
            var envelope = new OutboxEnvelope
            {
                OutboxEventId = evt.Id,
                EventType = evt.EventType,
                AggregateType = evt.AggregateType,
                AggregateId = evt.AggregateId,
                SchemaVersion = evt.SchemaVersion,
                Payload = evt.Payload,
                CorrelationId = evt.CorrelationId,
                PartitionKey = evt.PartitionKey,
                CreatedAtUtc = evt.CreatedAt
            };

            // Queue name cho Python consumer
            var endpoint = await _sendEndpointProvider.GetSendEndpoint(
                new Uri($"queue:{_settings.VectorIndexQueueName}"));

            await endpoint.Send(envelope, ct);
        }
    }
}
