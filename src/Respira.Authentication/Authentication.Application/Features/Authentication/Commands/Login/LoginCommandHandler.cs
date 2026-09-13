using Authentication.Application.Constracts.Authentication;
using Authentication.Application.Constracts.Data;
using Authentication.Application.Features.Authentication.Commands.Login.Result;
using Authentication.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Authentication.Application.Features.Authentication.Commands.Login
{
    public class LoginCommandHandler(
        IAuthDbContext dbContext,
        ILogger<LoginCommandHandler> logger,
        IHashService hashService,
        IJwtService jwtService
    ) : ICommandHandler<LoginCommand, Result<LoginResult?>>
    {
        public async Task<Result<LoginResult?>> HandleAsync(
            LoginCommand command,
            CancellationToken cancellationToken = default
        )
        {
            var account = await dbContext.Accounts.FirstOrDefaultAsync(a =>
                a.Email == command.Email && a.Status == StatusType.Active && !a.IsDeleted
            );

            if (account is null)
            {
                hashService.HashPassword(command.Password);
                logger.LogInformation("Email: {Email} login failed", command.Email);
                return Result<LoginResult?>.Failure(
                    new Error(ApplicationStatus.LoginFailure, "Email or password is incorrect")
                );
            }

            if (!hashService.VerifyPassword(command.Password, account.HashPassword))
            {
                logger.LogInformation("Email: {Email} login failed", command.Email);
                return Result<LoginResult?>.Failure(
                    new Error(ApplicationStatus.LoginFailure, "Email or password is incorrect")
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
