using Authentication.Application.Constracts.Authentication;
using Authentication.Application.Constracts.Data;
using Authentication.Application.Features.Authentication.Commands.Refresh.Result;
using Authentication.Domain.Entities;
using Authentication.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Authentication.Application.Features.Authentication.Commands.Refresh
{
    /// <summary>
    /// Rotates a valid refresh token and issues a replacement token pair for its active account.
    /// </summary>
    public class RefreshCommandHandler(
        IAuthDbContext dbContext,
        IJwtService jwtService,
        IHashService hashService,
        ILogger<RefreshCommandHandler> logger
    ) : ICommandHandler<RefreshCommand, Result<RefreshResult>>
    {
        /// <summary>
        /// Validates the persisted refresh token, invalidates it, and returns a newly issued token pair.
        /// </summary>
        /// <param name="command">The refresh request containing the raw refresh token.</param>
        /// <param name="cancellationToken">Token used to cancel database operations.</param>
        /// <returns>A replacement access/refresh token pair, or an unauthorized result.</returns>
        public async Task<Result<RefreshResult>> HandleAsync(
            RefreshCommand command,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(command);

            var refreshTokenHash = hashService.HashToken(command.RefreshToken);
            var refreshToken = await dbContext
                .Tokens.Where(x =>
                    x.HashToken == refreshTokenHash
                    && x.TokenType == TokenType.RefreshToken
                    && (x.ExpirationDate == null || x.ExpirationDate > DateTimeOffset.UtcNow)
                )
                .FirstOrDefaultAsync(cancellationToken);

            if (refreshToken is null)
            {
                logger.LogWarning("Refresh attempt used an invalid or expired refresh token");
                return Result<RefreshResult>.Failure(
                    new Error(ApplicationStatus.Unauthorized, "Invalid or expired refresh token")
                );
            }

            var user = await dbContext
                .Accounts.Where(x =>
                    x.Id == refreshToken.AccountId && x.Status == StatusType.Active && !x.IsDeleted
                )
                .FirstOrDefaultAsync(cancellationToken);

            if (user is null)
            {
                logger.LogWarning(
                    "Refresh attempt rejected because account {AccountId} is unavailable",
                    refreshToken.AccountId
                );
                return Result<RefreshResult>.Failure(
                    new Error(ApplicationStatus.Unauthorized, "Invalid or expired refresh token")
                );
            }

            var accessToken = jwtService.GenerateAccessToken(user);
            var newRefreshToken = await jwtService.GenerateRefreshTokenAsync(user);

            dbContext.Tokens.Remove(refreshToken);
            await dbContext.BlacklistTokens.AddAsync(
                new BlacklistToken()
                {
                    HashToken = refreshToken.HashToken,
                    Reason = "Refresh token rotation",
                    ExpirationDate = refreshToken.ExpirationDate,
                }
            );

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result<RefreshResult>.Success(
                statusCode: ApplicationStatus.Success,
                data: new RefreshResult()
                {
                    AccessToken = accessToken,
                    RefreshToken = newRefreshToken,
                }
            );
        }
    }
}
