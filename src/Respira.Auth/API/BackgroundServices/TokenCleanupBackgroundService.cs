using Application.Abstracts;
using Application.Features.Tokens.Commands.RemoveExpiredTokens;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wolverine;

namespace Respira.Auth.API.BackgroundServices;

/// <summary>
/// Background service that periodically revokes expired tokens by dispatching
/// <c>RemoveExpiredTokensCommand</c> on a configurable interval. Waits for host
/// startup before the first run.
/// </summary>
public class TokenCleanupBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<TokenCleanupBackgroundService> logger,
    IOptions<TokenCleanupOption> options,
    IHostApplicationLifetime lifetime
) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(options.Value.IntervalMinutes);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await WaitForHostStartupAsync(stoppingToken);

        await CleanupAsync(stoppingToken);

        using var timer = new PeriodicTimer(_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CleanupAsync(stoppingToken);
        }
    }

    private async Task WaitForHostStartupAsync(CancellationToken stoppingToken)
    {
        if (lifetime.ApplicationStarted.IsCancellationRequested)
        {
            return;
        }

        var tcs = new TaskCompletionSource();
        await using var registration = lifetime.ApplicationStarted.Register(() => tcs.TrySetResult());
        await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, stoppingToken));
    }

    private async Task CleanupAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

            var removed = await bus.InvokeAsync<int>(
                new RemoveExpiredTokensCommand(),
                stoppingToken
            );
            if (removed > 0)
            {
                logger.LogInformation("Expired-token cleanup removed {Count} rows", removed);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Expired-token cleanup failed");
        }
    }
}
