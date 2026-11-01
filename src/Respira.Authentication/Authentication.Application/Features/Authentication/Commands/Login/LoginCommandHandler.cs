using Authentication.Application.Constracts.Authentication;
using Authentication.Application.Constracts.Cache;
using Authentication.Application.Constracts.Data;
using Authentication.Application.Features.Authentication.Commands.Login.Result;
using Authentication.Domain.Entities;
using Authentication.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;
using Respira.ServiceDefaults.Contracts.Messages.Audit;
using Wolverine;

namespace Authentication.Application.Features.Authentication.Commands.Login
{
    public class LoginCommandHandler(
        IAuthDbContext dbContext,
        ILogger<LoginCommandHandler> logger,
        IHashService hashService,
        IJwtService jwtService,
        ICacheService cacheService,
        IMessageBus bus
    ) : ICommandHandler<LoginCommand, Result<LoginResult?>>
    {
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

        public async Task<Result<LoginResult?>> HandleAsync(
            LoginCommand command,
            CancellationToken cancellationToken = default
        )
        {
            var accountCacheKey = $"auth:account:email:{command.Email.ToLowerInvariant()}";
            var account = await cacheService.GetAsync<Account>(accountCacheKey, cancellationToken);

            if (account is null)
            {
                account = await dbContext.Accounts.FirstOrDefaultAsync(a =>
                    a.Email == command.Email && a.Status == StatusType.Active && !a.IsDeleted
                );

                if (account is not null)
                {
                    await cacheService.SetAsync(
                        accountCacheKey,
                        account,
                        CacheExpiration,
                        cancellationToken
                    );
                }
            }

            if (account is null)
            {
                hashService.HashPassword(command.Password);
                logger.LogInformation("Email: {Email} login failed", command.Email);
                await bus.PublishAsync(new AuditEvent
                {
                    ActorRole = "Anonymous",
                    Action = "Authentication.LoginFailed",
                    EntityType = "Account",
                    DetailsJson = "{\"reason\":\"invalid_credentials\"}",
                });
                return Result<LoginResult?>.Failure(
                    new Error(ApplicationStatus.LoginFailure, "Email or password is incorrect")
                );
            }

            if (!hashService.VerifyPassword(command.Password, account.HashPassword))
            {
                logger.LogInformation("Email: {Email} login failed", command.Email);
                await bus.PublishAsync(new AuditEvent
                {
                    ActorAccountId = account.Id,
                    ActorRole = account.Role.ToString(),
                    Action = "Authentication.LoginFailed",
                    EntityType = "Account",
                    EntityId = account.Id,
                    DetailsJson = "{\"reason\":\"invalid_credentials\"}",
                });
                return Result<LoginResult?>.Failure(
                    new Error(ApplicationStatus.LoginFailure, "Email or password is incorrect")
                );
            }

            var accessToken = jwtService.GenerateAccessToken(account);
            var refreshToken = await jwtService.GenerateRefreshTokenAsync(account);

            await bus.PublishAsync(new AuditEvent
            {
                ActorAccountId = account.Id,
                ActorRole = account.Role.ToString(),
                Action = "Authentication.LoginSucceeded",
                EntityType = "Account",
                EntityId = account.Id,
            });

            return Result<LoginResult?>.Success(
                ApplicationStatus.Success,
                new LoginResult() { AccessToken = accessToken, RefreshToken = refreshToken }
            );
        }
    }
}
