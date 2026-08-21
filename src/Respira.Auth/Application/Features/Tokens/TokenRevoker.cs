using Application.Abstracts.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Tokens;

/// <summary>
/// Shared revocation logic used by both <c>RemoveTokenCommand</c> (logout) and the
/// expired-token cleanup. Moves a token into the blacklist and removes the issued-token
/// row. Staged changes are not saved; the caller is responsible for persistence.
/// </summary>
/// <param name="dbContext">Auth database context</param>
public class TokenRevoker(IAuthDbContext dbContext)
{
    /// <summary>
    /// Stages revocation of the token identified by its hash: adds a blacklist entry and
    /// removes the token row (if not already blacklisted / present).
    /// </summary>
    /// <param name="hashToken">SHA-256 hash of the token</param>
    /// <param name="reason">Revocation reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if a revocation was staged, false if the token was missing or already blacklisted</returns>
    public async Task<bool> TryStageRevocationAsync(
        string hashToken,
        string reason,
        CancellationToken cancellationToken = default
    )
    {
        if (await dbContext.BlacklistTokens.AnyAsync(b => b.HashToken == hashToken, cancellationToken))
        {
            return false;
        }

        var token = await dbContext.Tokens.FirstOrDefaultAsync(
            t => t.HashToken == hashToken,
            cancellationToken
        );
        if (token is null)
        {
            return false;
        }

        dbContext.BlacklistTokens.Add(
            new BlacklistToken
            {
                HashToken = token.HashToken,
                ExpirationDate = token.ExpirationDate,
                Reason = reason,
            }
        );
        dbContext.Tokens.Remove(token);
        return true;
    }
}
