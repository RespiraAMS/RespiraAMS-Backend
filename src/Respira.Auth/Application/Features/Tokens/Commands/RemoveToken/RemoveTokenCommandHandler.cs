using Application.Abstracts.Authentication;
using Application.Abstracts.Data;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Constracts.CQRS;
using Respira.ServiceDefaults.Exceptions;

namespace Application.Features.Tokens.Commands.RemoveToken
{
    /// <summary>
/// Handles <see cref="RemoveTokenCommand"/>: revokes a token by moving it to the blacklist and
/// removing the issued-token row (via <see cref="TokenRevoker"/>). Used for logout/password-reset
/// revocation. Returns true when a revocation was persisted.
/// </summary>
public class RemoveTokenCommandHandler(
        IAuthDbContext dbContext,
        TokenRevoker tokenRevoker,
        ILogger<RemoveTokenCommand> logger,
        IHashService hashService
    ) : ICommandHandler<RemoveTokenCommand, bool>
    {
        /// <summary>
        /// Stages and persists revocation of the supplied token.
        /// </summary>
        /// <param name="command">Token to revoke and the revocation reason.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the token was revoked, false if missing or already blacklisted.</returns>
        public async Task<bool> HandleAsync(
            RemoveTokenCommand command,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var hashToken = hashService.HashToken(command.Token);
                var staged = await tokenRevoker.TryStageRevocationAsync(
                    hashToken,
                    command.Reason,
                    cancellationToken
                );
                if (!staged)
                {
                    logger.LogDebug("Token removal failed: token not found or already blacklisted");
                    return false;
                }

                if (await dbContext.SaveChangesAsync() <= 0)
                {
                    logger.LogError("Failed to persist token revocation");
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to remove token");
                throw new ServerException();
            }
        }
    }
}
