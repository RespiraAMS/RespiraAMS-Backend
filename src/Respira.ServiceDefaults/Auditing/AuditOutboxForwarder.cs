using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Respira.ServiceDefaults.Auditing;

/// <summary>
/// Polls the local audit outbox and forwards pending rows to the central Saga
/// audit queue. Failures leave <see cref="AuditEntry.ForwardedAt"/> null so the
/// row is retried on the next tick.
/// </summary>
public sealed class AuditOutboxForwarder<TDbContext>(
    IServiceScopeFactory scopeFactory,
    ILogger<AuditOutboxForwarder<TDbContext>> logger
) : BackgroundService
    where TDbContext : DbContext
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private const int BatchSize = 100;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                    break;
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                await ForwardBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to forward local audit entries");
            }
        }
    }

    private async Task ForwardBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IAuditEventPublisher>();

        var pending = await dbContext
            .Set<AuditEntry>()
            .Where(entry => entry.ForwardedAt == null)
            .OrderBy(entry => entry.OccurredAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
            return;

        var forwarded = new List<AuditEntry>(pending.Count);

        foreach (var entry in pending)
        {
            try
            {
                await publisher.PublishAsync(AuditEventFactory.Create(entry), cancellationToken);
                entry.ForwardedAt = DateTimeOffset.UtcNow;
                forwarded.Add(entry);
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Failed to publish audit entry {AuditEntryId}; will retry",
                    entry.Id
                );
                break;
            }
        }

        if (forwarded.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogDebug("Forwarded {AuditEntryCount} audit entries", forwarded.Count);
        }
    }
}
