using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Respira.Authentication.Application.Contracts.Authentication;
using Respira.Authentication.Application.Contracts.Cache;
using Respira.Authentication.Application.Contracts.Data;
using Respira.Authentication.Domain.Entities;
using Respira.Authentication.Domain.Enums;
using Respira.Authentication.Domain.Policies;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Messages.Audit;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Authentication.Application.Features.Authentication.Commands.ResetPassword
{
    /// <summary>
    /// Consumes a single-use password reset token and stores the new password.
    /// Every refresh token of the account is revoked at the same time, so a session
    /// opened before the reset cannot be used anymore.
    /// </summary>
    public class ResetPasswordCommandHandler(
        IAuthDbContext dbContext,
        IHashService hashService,
        ICacheService cacheService,
        IOptions<JwtOption> jwtOption,
        ILogger<ResetPasswordCommandHandler> logger,
        IMessageBus bus
    ) : ICommandHandler<ResetPasswordCommand, Result<bool>>
    {
        /// <summary>
        /// Validates the raw token, stores the new password and revokes the sessions
        /// opened with the previous one.
        /// </summary>
        /// <param name="command">The request containing the raw token and the new password.</param>
        /// <param name="cancellationToken">Token used to cancel database operations.</param>
        /// <returns>True when the password was changed, otherwise a failure result.</returns>
        public async Task<Result<bool>> HandleAsync(
            ResetPasswordCommand command,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(command);

            if (string.IsNullOrWhiteSpace(command.Token))
            {
                return Failure(ApplicationStatus.BadRequest, "A password reset token is required");
            }

            if (!string.Equals(command.NewPassword, command.ConfirmPassword, StringComparison.Ordinal))
            {
                return Failure(
                    ApplicationStatus.BadRequest,
                    "The new password and its confirmation do not match"
                );
            }

            var violations = PasswordPolicy.GetViolations(command.NewPassword);
            if (violations.Count > 0)
            {
                return Failure(
                    ApplicationStatus.BusinessRuleViolation,
                    string.Join("; ", violations)
                );
            }

            var tokenHash = hashService.HashToken(command.Token.Trim());
            var resetToken = await dbContext
                .Tokens.Where(x =>
                    x.HashToken == tokenHash
                    && x.TokenType == TokenType.PasswordResetToken
                    && (x.ExpirationDate == null || x.ExpirationDate > DateTimeOffset.UtcNow)
                )
                .FirstOrDefaultAsync(cancellationToken);

            if (resetToken is null)
            {
                logger.LogWarning("Password reset attempt used an invalid or expired token");
                return Failure(
                    ApplicationStatus.Unauthorized,
                    "Invalid or expired password reset token"
                );
            }

            var account = await dbContext
                .Accounts.Where(x =>
                    x.Id == resetToken.AccountId
                    && x.Status == StatusType.Active
                    && !x.IsDeleted
                )
                .FirstOrDefaultAsync(cancellationToken);

            if (account is null)
            {
                logger.LogWarning(
                    "Password reset attempt rejected because account {AccountId} is unavailable",
                    resetToken.AccountId
                );
                return Failure(
                    ApplicationStatus.Unauthorized,
                    "Invalid or expired password reset token"
                );
            }

            account.HashPassword = hashService.HashPassword(command.NewPassword);
            account.UpdatedAt = DateTimeOffset.UtcNow;

            // Single-use: every outstanding reset token dies with this attempt.
            var resetTokens = await dbContext
                .Tokens.Where(x =>
                    x.AccountId == account.Id && x.TokenType == TokenType.PasswordResetToken
                )
                .ToListAsync(cancellationToken);

            dbContext.Tokens.RemoveRange(resetTokens);

            // The password changed, so the sessions opened with the old one are revoked.
            var refreshTokens = await dbContext
                .Tokens.Where(x =>
                    x.AccountId == account.Id && x.TokenType == TokenType.RefreshToken
                )
                .ToListAsync(cancellationToken);

            foreach (var refreshToken in refreshTokens)
            {
                dbContext.BlacklistTokens.Add(
                    new BlacklistToken
                    {
                        HashToken = refreshToken.HashToken,
                        Reason = "Password reset",
                        ExpirationDate = refreshToken.ExpirationDate
                            ?? DateTimeOffset.UtcNow.AddDays(jwtOption.Value.RefreshTokenExpires),
                    }
                );
            }

            dbContext.Tokens.RemoveRange(refreshTokens);

            await dbContext.SaveChangesAsync(cancellationToken);

            // The cached account copies still carry the previous password hash.
            await cacheService.RemoveAsync(
                $"auth:account:email:{account.Email.ToLowerInvariant()}",
                cancellationToken
            );
            await cacheService.RemoveAsync($"auth:account:id:{account.Id}", cancellationToken);

            logger.LogInformation("Password reset completed for account {AccountId}", account.Id);

            await bus.PublishAsync(new AuditEvent
            {
                ActorAccountId = account.Id,
                ActorRole = account.Role.ToString(),
                Action = "Authentication.PasswordReset",
                EntityType = "Account",
                EntityId = account.Id,
            });

            return Result<bool>.Success(ApplicationStatus.Success, true);
        }

        private static Result<bool> Failure(string statusCode, string description)
        {
            return Result<bool>.Failure(new ApplicationError(statusCode, description));
        }
    }
}
