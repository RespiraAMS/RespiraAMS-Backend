using Authentication.Application.Constracts.Authentication;
using Authentication.Application.Constracts.Cache;
using Authentication.Application.Constracts.Data;
using Authentication.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Authentication.Application.Features.Authentication.Commands.VerifyEmail
{
    /// <summary>
    /// Consumes a single-use email verification token and flags the owning account
    /// as confirmed. Only the SHA-256 hash of the token is compared, mirroring the
    /// way SendVerify stores it.
    /// </summary>
    public class VerifyEmailCommandHandler(
        IAuthDbContext dbContext,
        IHashService hashService,
        ICacheService cacheService,
        ILogger<VerifyEmailCommandHandler> logger
    ) : ICommandHandler<VerifyEmailCommand, Result<bool>>
    {
        /// <summary>
        /// Validates the raw token, marks the account's address as confirmed, and
        /// burns every verification token the account holds.
        /// </summary>
        /// <param name="command">The verification request containing the raw token.</param>
        /// <param name="cancellationToken">Token used to cancel database operations.</param>
        /// <returns>True when the address is confirmed, otherwise a failure result.</returns>
        public async Task<Result<bool>> HandleAsync(
            VerifyEmailCommand command,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(command);

            if (string.IsNullOrWhiteSpace(command.Token))
            {
                return Failure(ApplicationStatus.BadRequest, "A verification token is required");
            }

            var tokenHash = hashService.HashToken(command.Token.Trim());
            var verificationToken = await dbContext
                .Tokens.Where(x =>
                    x.HashToken == tokenHash
                    && x.TokenType == TokenType.EmailVerificationToken
                    && (x.ExpirationDate == null || x.ExpirationDate > DateTimeOffset.UtcNow)
                )
                .FirstOrDefaultAsync(cancellationToken);

            if (verificationToken is null)
            {
                logger.LogWarning("Verification attempt used an invalid or expired token");
                return Failure(
                    ApplicationStatus.Unauthorized,
                    "Invalid or expired verification token"
                );
            }

            var account = await dbContext
                .Accounts.Where(x =>
                    x.Id == verificationToken.AccountId
                    && x.Status == StatusType.Active
                    && !x.IsDeleted
                )
                .FirstOrDefaultAsync(cancellationToken);

            if (account is null)
            {
                logger.LogWarning(
                    "Verification attempt rejected because account {AccountId} is unavailable",
                    verificationToken.AccountId
                );
                return Failure(
                    ApplicationStatus.Unauthorized,
                    "Invalid or expired verification token"
                );
            }

            // Single-use: every outstanding verification token dies with this attempt,
            // including any older ones a previous SendVerify left behind.
            var verificationTokens = await dbContext
                .Tokens.Where(x =>
                    x.AccountId == account.Id && x.TokenType == TokenType.EmailVerificationToken
                )
                .ToListAsync(cancellationToken);

            dbContext.Tokens.RemoveRange(verificationTokens);

            if (account.IsEmailConfirmed)
            {
                // Re-opening an already confirmed link (page reload, second click)
                // reaches the same end state, so it is reported as a success.
                logger.LogInformation(
                    "Verification token for the already confirmed address {Email} consumed",
                    account.Email
                );
            }
            else
            {
                account.IsEmailConfirmed = true;
                account.UpdatedAt = DateTimeOffset.UtcNow;
                logger.LogInformation("Email address {Email} verified", account.Email);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            // The cached account copies still carry the previous confirmation flag.
            await cacheService.RemoveAsync(
                $"auth:account:email:{account.Email.ToLowerInvariant()}",
                cancellationToken
            );
            await cacheService.RemoveAsync(
                $"auth:account:id:{account.Id}",
                cancellationToken
            );

            return Result<bool>.Success(ApplicationStatus.Success, true);
        }

        private static Result<bool> Failure(string statusCode, string description)
        {
            return Result<bool>.Failure(new Error(statusCode, description));
        }
    }
}
