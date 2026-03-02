using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Viren.Repositories.Domains;
using Viren.Repositories.Enums;
using Viren.Repositories.Interfaces;
using Viren.Services.IntegrationEvents;
using Viren.Services.Outbox;

namespace Viren.Services.Workers
{
    public sealed class OutboxPublisherWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxPublisherWorker> _logger;

        private const int BatchSize = 25;
        private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan BusyDelay = TimeSpan.FromMilliseconds(200);
        private const int MaxRetry = 12;

        // TEST: tạo 1 message mẫu duy nhất khi worker start (để nhìn thấy worker chạy)
        private const bool SeedTestMessageOnStart = true;
        private static bool _seeded = false;

        public OutboxPublisherWorker(IServiceScopeFactory scopeFactory, ILogger<OutboxPublisherWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("OutboxPublisherWorker STARTED at {UtcNow}", DateTime.UtcNow);

            if (SeedTestMessageOnStart && !_seeded)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var repo = uow.GetRepository<OutboxEvent, Guid>();

                    // tạo 1 outbox event mẫu để test pipeline
                    var evt = new OutboxEvent
                    {
                        Id = Guid.NewGuid(),
                        AggregateType = "Test",
                        AggregateId = Guid.NewGuid(),
                        EventType = "Test.Ping",
                        Payload = "{\"message\":\"hello from outbox test\"}",
                        SchemaVersion = 1,
                        Status = OutboxStatus.Pending,
                        RetryCount = 0,
                        NextRetryAt = null,
                        LastError = null,
                        CorrelationId = Guid.NewGuid(),
                        PartitionKey = "test",
                        CreatedAt = DateTime.UtcNow,
                        PublishedAt = null
                    };

                    await repo.AddAsync(evt, cancellationToken);
                    await uow.SaveChangesAsync(cancellationToken);

                    _seeded = true;
                    _logger.LogWarning("Seeded TEST outbox event: {OutboxId}", evt.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to seed test outbox event");
                }
            }

            await base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogWarning("OutboxPublisherWorker STOPPED at {UtcNow}", DateTime.UtcNow);
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var delay = IdleDelay;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var processed = await ProcessOnce(stoppingToken);
                    delay = processed > 0 ? BusyDelay : IdleDelay;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "OutboxPublisherWorker loop error");
                    delay = IdleDelay;
                }

                await Task.Delay(delay, stoppingToken);
            }
        }

        private async Task<int> ProcessOnce(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();

            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventBusPublisher>();
            var repo = uow.GetRepository<OutboxEvent, Guid>();

            var now = DateTime.UtcNow;

            // Heartbeat (debug)
            _logger.LogDebug("Outbox worker heartbeat {UtcNow}", now);

            // (optional) log nhanh số lượng pending
            var pendingCount = await repo.Query()
                .AsNoTracking()
                .CountAsync(x => x.Status == OutboxStatus.Pending
                              && (x.NextRetryAt == null || x.NextRetryAt <= now), ct);

            if (pendingCount == 0)
            {
                _logger.LogDebug("Outbox: no pending events");
                return 0;
            }

            // 1) lấy candidate ids
            var candidateIds = await repo.Query()
                .AsNoTracking()
                .Where(x => x.Status == OutboxStatus.Pending
                            && (x.NextRetryAt == null || x.NextRetryAt <= now))
                .OrderBy(x => x.CreatedAt)
                .Select(x => x.Id)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (candidateIds.Count == 0) return 0;

            // 2) claim
            var claimedCount = await repo.Query()
                .Where(x => candidateIds.Contains(x.Id) && x.Status == OutboxStatus.Pending)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, OutboxStatus.Processing), ct);

            if (claimedCount == 0)
            {
                _logger.LogDebug("Outbox: candidates={Candidates}, claimed=0 (another worker may have claimed)",
                    candidateIds.Count);
                return 0;
            }

            // 3) load claimed
            var events = await repo.Query()
                .Where(x => candidateIds.Contains(x.Id) && x.Status == OutboxStatus.Processing)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync(ct);

            _logger.LogInformation("Outbox: pending={Pending}, candidates={Candidates}, claimed={Claimed}, loaded={Loaded}",
                pendingCount, candidateIds.Count, claimedCount, events.Count);

            var published = 0;
            var failed = 0;

            foreach (var evt in events)
            {
                try
                {
                    await publisher.PublishAsync(evt, ct);

                    evt.Status = OutboxStatus.Published;
                    evt.PublishedAt = now;
                    evt.LastError = null;
                    evt.NextRetryAt = null;

                    published++;
                }
                catch (Exception ex)
                {
                    evt.RetryCount += 1;
                    evt.LastError = ex.Message;

                    if (evt.RetryCount >= MaxRetry)
                    {
                        evt.Status = OutboxStatus.Failed;
                        evt.NextRetryAt = null;
                    }
                    else
                    {
                        evt.Status = OutboxStatus.Pending;
                        evt.NextRetryAt = now.AddSeconds(ComputeBackoffSeconds(evt.RetryCount));
                    }

                    failed++;

                    _logger.LogWarning(ex,
                        "Outbox publish failed: Id={Id} Type={Type} Retry={RetryCount}",
                        evt.Id, evt.EventType, evt.RetryCount);
                }
            }

            await uow.SaveChangesAsync(ct);

            _logger.LogInformation("Outbox: published={Published}, failed={Failed}", published, failed);

            return published + failed;
        }

        private static int ComputeBackoffSeconds(int retryCount)
        {
            var r = Math.Clamp(retryCount, 1, 12);
            return r switch
            {
                1 => 2,
                2 => 5,
                3 => 10,
                4 => 30,
                5 => 60,
                6 => 120,
                7 => 300,
                8 => 600,
                9 => 900,
                _ => 1800
            };
        }
    }
}
