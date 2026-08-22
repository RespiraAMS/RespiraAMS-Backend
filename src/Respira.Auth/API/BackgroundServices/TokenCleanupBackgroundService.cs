using Application.Abstracts;
using Application.Features.Tokens.Commands.RemoveExpiredTokens;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wolverine;

namespace Respira.Auth.API.BackgroundServices;

/// <summary>
/// Background service that periodically invokes <see cref="RemoveExpiredTokensCommand"/>
/// to delete expired tokens (and expired blacklist entries) from the database.
/// </summary>
/// <param name="bus">Wolverine message bus used to dispatch the cleanup command</param>
/// <param name="logger">Logger</param>
/// <param name="options">Cleanup interval configuration</param>
public class TokenCleanupBackgroundService(
    IMessageBus bus,
    ILogger<TokenCleanupBackgroundService> logger,
    IOptions<TokenCleanupOption> options
) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(options.Value.IntervalMinutes);

    /// <summary>
    /// Runs an initial cleanup, then repeats on a fixed interval until the host stops.
    /// </summary>
    /// <param name="stoppingToken">Token cancelled when the host is shutting down</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await CleanupAsync(stoppingToken);

        using var timer = new PeriodicTimer(_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CleanupAsync(stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken stoppingToken)
    {
        try
        {
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
