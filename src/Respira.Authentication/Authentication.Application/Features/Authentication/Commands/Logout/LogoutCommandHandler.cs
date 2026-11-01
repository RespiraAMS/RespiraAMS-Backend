using Authentication.Application.Constracts.Authentication;
using Authentication.Application.Constracts.Cache;
using Authentication.Application.Constracts.Data;
using Authentication.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;
using Respira.ServiceDefaults.Contracts.Messages.Audit;
using Wolverine;

namespace Authentication.Application.Features.Authentication.Commands.Logout
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
                .Tokens.Where(x => x.HashToken == refreshTokenHash)
                .FirstOrDefaultAsync(cancellationToken);

            if (isTokenExist is null)
            {
                logger.LogInformation("Invalid token");
                return Result<bool>.Failure(
                    new Error(ApplicationStatus.Unauthorized, "Invalid token")
                );
            }

            var account = await dbContext.Accounts.FirstOrDefaultAsync(
                a => a.Id == isTokenExist.AccountId && !a.IsDeleted,
                cancellationToken
            );

            var blacklistToken = new BlacklistToken()
            {
                HashToken = hashService.HashToken(command.RefreshToken),
                Reason = "Logout",
                ExpirationDate = DateTimeOffset.UtcNow.AddDays(jwtOption.Value.RefreshTokenExpires),
            };

            dbContext.Tokens.Remove(isTokenExist);
            dbContext.BlacklistTokens.Add(blacklistToken);

            await dbContext.SaveChangesAsync(cancellationToken);

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
