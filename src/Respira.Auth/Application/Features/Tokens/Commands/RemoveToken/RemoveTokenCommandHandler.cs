using Application.Abstracts.Authentication;
using Application.Abstracts.Data;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Constracts.CQRS;
using Respira.ServiceDefaults.Exceptions;

namespace Application.Features.Tokens.Commands.RemoveToken
{
    public class RemoveTokenCommandHandler(
        IAuthDbContext dbContext,
        TokenRevoker tokenRevoker,
        ILogger<RemoveTokenCommand> logger,
        IHashService hashService
    ) : ICommandHandler<RemoveTokenCommand, bool>
    {
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
