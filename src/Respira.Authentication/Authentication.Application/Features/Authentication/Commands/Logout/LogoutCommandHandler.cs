using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Respira.Authentication.Application.Contracts.Authentication;
using Respira.Authentication.Application.Contracts.Cache;
using Respira.Authentication.Application.Contracts.Data;
using Respira.Authentication.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;
using Respira.ServiceDefaults.Contracts.Messages.Audit;
using Wolverine;

namespace Respira.Authentication.Application.Features.Authentication.Commands.Logout
{
    public class LogoutCommandHandler(
        IAuthDbContext dbContext,
        IHashService hashService,
        ILogger<LogoutCommandHandler> logger,
        IOptions<JwtOption> jwtOption,
        ICacheService cacheService,
        IMessageBus bus
    ) : ICommandHandler<LogoutCommand, Result<bool>>
    {
        public async Task<Result<bool>> HandleAsync(
            LogoutCommand command,
            CancellationToken cancellationToken = default
        )
        {
            var refreshTokenHash = hashService.HashToken(command.RefreshToken);
            var isTokenExist = await dbContext
                .Tokens.AsNoTracking()
                .Where(x => x.HashToken == refreshTokenHash)
                .FirstOrDefaultAsync(cancellationToken);

            if (isTokenExist is null)
            {
                logger.LogInformation("Invalid token");
                return Result<bool>.Failure(
                    new ApplicationError(ApplicationStatus.Unauthorized, "Invalid token")
                );
            }

            var account = await dbContext
                .Accounts.AsNoTracking()
                .FirstOrDefaultAsync(
                    a => a.Id == isTokenExist.AccountId && !a.IsDeleted,
                    cancellationToken
                );

            // Claim the token with a set-based DELETE. When a concurrent refresh/logout
            // already revoked it the statement affects 0 rows, which is the same end state:
            // logging out twice is a success.
            var consumed = await dbContext
                .Tokens.Where(x => x.Id == isTokenExist.Id)
                .ExecuteDeleteAsync(cancellationToken);

            if (consumed == 0)
            {
                logger.LogInformation(
                    "Refresh token for account {AccountId} was already revoked by a concurrent request",
                    isTokenExist.AccountId
                );
            }
            else
            {
                dbContext.BlacklistTokens.Add(
                    new BlacklistToken
                    {
                        HashToken = refreshTokenHash,
                        Reason = "Logout",
                        ExpirationDate = DateTimeOffset.UtcNow.AddDays(
                            jwtOption.Value.RefreshTokenExpires
                        ),
                    }
                );

                await dbContext.SaveChangesAsync(cancellationToken);
            }

            if (account is not null)
            {
                await cacheService.RemoveAsync(
                    $"auth:account:email:{account.Email.ToLowerInvariant()}",
                    cancellationToken
                );
            }

            logger.LogInformation("User logged out successfully");
            await bus.PublishAsync(new AuditEvent
            {
                ActorAccountId = isTokenExist.AccountId,
                ActorRole = account?.Role.ToString() ?? "Unknown",
                Action = "Authentication.LogoutSucceeded",
                EntityType = "Account",
                EntityId = isTokenExist.AccountId,
            });
            return Result<bool>.Success(ApplicationStatus.Success, true);
        }
    }
}
