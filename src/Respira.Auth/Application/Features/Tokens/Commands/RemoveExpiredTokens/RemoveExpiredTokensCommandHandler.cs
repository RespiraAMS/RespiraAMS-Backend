using Application.Abstracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Tokens.Commands.RemoveExpiredTokens;

/// <summary>
/// Revokes expired tokens by reusing the same blacklist-transfer logic as
/// <see cref="RemoveTokenCommand"/> (via <see cref="TokenRevoker"/>), then removes
/// them from the tokens table. Expired blacklist entries are also purged as housekeeping.
/// Intended to be invoked periodically by a background service.
/// </summary>
/// <param name="dbContext">Auth database context</param>
/// <param name="tokenRevoker">Shared token revocation logic (same as RemoveToken)</param>
/// <param name="logger">Logger</param>
public class RemoveExpiredTokensCommandHandler(
    IAuthDbContext dbContext,
    TokenRevoker tokenRevoker,
    ILogger<RemoveExpiredTokensCommandHandler> logger
) : ICommandHandler<RemoveExpiredTokensCommand, int>
{
    private const string ExpiredReason = "expired";

    /// <summary>
    /// Stages revocation (blacklist + remove) of each expired token, then saves once.
    /// </summary>
    /// <param name="command">Cleanup command (no parameters)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of expired token rows revoked</returns>
    public async Task<int> HandleAsync(
        RemoveExpiredTokensCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var now = DateTimeOffset.UtcNow;

        var expiredTokens = await dbContext
            .Tokens.Where(t => t.ExpirationDate != null && t.ExpirationDate < now)
            .ToListAsync(cancellationToken);

        foreach (var token in expiredTokens)
        {
            await tokenRevoker.TryStageRevocationAsync(
                token.HashToken,
                ExpiredReason,
                cancellationToken
            );
        }

        // Housekeeping: purge blacklist entries that have themselves expired
        var expiredBlacklist = await dbContext
            .BlacklistTokens.Where(t => t.ExpirationDate != null && t.ExpirationDate < now)
            .ToListAsync(cancellationToken);
        dbContext.BlacklistTokens.RemoveRange(expiredBlacklist);

        var revoked = expiredTokens.Count;
        if (revoked > 0)
        {
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Revoked {Count} expired tokens into the blacklist", revoked);
        }

        return revoked;
    }
}
