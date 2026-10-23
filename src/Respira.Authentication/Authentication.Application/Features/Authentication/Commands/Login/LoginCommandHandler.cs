using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Authentication.Application.Constracts.Authentication;
using Respira.Authentication.Application.Constracts.Cache;
using Respira.Authentication.Application.Constracts.Data;
using Respira.Authentication.Application.Features.Authentication.Commands.Login.Result;
using Respira.Authentication.Domain.Entities;
using Respira.Authentication.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Authentication.Application.Features.Authentication.Commands.Login
{
    public class LoginCommandHandler(
        IAuthDbContext dbContext,
        ILogger<LoginCommandHandler> logger,
        IHashService hashService,
        IJwtService jwtService,
        ICacheService cacheService
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
                account = await dbContext.Accounts
                    .FirstOrDefaultAsync(a => a.Email == command.Email && a.Status == StatusType.Active && !a.IsDeleted, cancellationToken);

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
                return Result<LoginResult?>.Failure(
                    new ApplicationError(ApplicationStatus.LoginFailure, "Email or password is incorrect")
                );
            }

            if (!hashService.VerifyPassword(command.Password, account.HashPassword))
            {
                logger.LogInformation("Email: {Email} login failed", command.Email);
                return Result<LoginResult?>.Failure(
                    new ApplicationError(ApplicationStatus.LoginFailure, "Email or password is incorrect")
                );
            }

            var accessToken = jwtService.GenerateAccessToken(account);
            var refreshToken = await jwtService.GenerateRefreshTokenAsync(account);

            return Result<LoginResult?>.Success(
                ApplicationStatus.Success,
                new LoginResult() { AccessToken = accessToken, RefreshToken = refreshToken }
            );
        }
    }
}
