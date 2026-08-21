using Application.Abstracts.Authentication;
using Application.Abstracts.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Constracts.CQRS;
using Respira.ServiceDefaults.Exceptions;

namespace Application.Features.Authentication.Logout;

/// <summary>
/// Revokes the access and refresh tokens on logout by adding them to the
/// blacklist and removing the persisted refresh token row.
/// </summary>
/// <param name="dbContext">Auth database context</param>
/// <param name="hashService">Token hashing utility</param>
/// <param name="logger">Logger</param>
public class LogoutCommandHandler(
    IAuthDbContext dbContext,
    IHashService hashService,
    ILogger<LogoutCommandHandler> logger
) : ICommandHandler<LogoutCommand, bool>
{
    private const string LogoutReason = "logout";

    /// <summary>
    /// Blacklists the access and refresh tokens and removes the refresh token row.
    /// </summary>
    /// <param name="command">Logout tokens</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the revocation was persisted</returns>
    public async Task<bool> HandleAsync(
        LogoutCommand command,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var accessHash = hashService.HashToken(command.AccessToken);
            var refreshHash = hashService.HashToken(command.RefreshToken);

            var alreadyBlacklisted = await dbContext
                .BlacklistTokens.Where(x => x.HashToken == accessHash || x.HashToken == refreshHash)
                .Select(x => x.HashToken)
                .ToListAsync(cancellationToken);

            if (!alreadyBlacklisted.Contains(accessHash))
            {
                dbContext.BlacklistTokens.Add(
                    new BlacklistToken { HashToken = accessHash, Reason = LogoutReason }
                );
            }

            if (!alreadyBlacklisted.Contains(refreshHash))
            {
                dbContext.BlacklistTokens.Add(
                    new BlacklistToken { HashToken = refreshHash, Reason = LogoutReason }
                );
            }

            var refreshToken = await dbContext.Tokens.FirstOrDefaultAsync(
                x => x.HashToken == refreshHash,
                cancellationToken
            );
            if (refreshToken is not null)
            {
                dbContext.Tokens.Remove(refreshToken);
            }

            if (await dbContext.SaveChangesAsync() <= 0)
            {
                logger.LogWarning("Logout: no token revocation changes were persisted");
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to logout and revoke tokens");
            throw new ServerException();
        }
    }
}
