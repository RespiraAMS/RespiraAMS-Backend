using System.Net;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Respira.Authentication.Application.Contracts.Authentication;
using Respira.Authentication.Application.Contracts.Cache;
using Respira.Authentication.Application.Contracts.Data;
using Respira.Authentication.Application.Contracts.Email;
using Respira.Authentication.Domain.Entities;
using Respira.Authentication.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Messages.Audit;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Authentication.Application.Features.Authentication.Commands.ForgotPassword
{
    /// <summary>
    /// Issues a single-use password reset token for an account and emails a reset
    /// link to its address. Only the SHA-256 hash of the token is persisted; the raw
    /// token exists only in the email that is sent out.
    /// </summary>
    public class ForgotPasswordCommandHandler(
        IAuthDbContext dbContext,
        IHashService hashService,
        IEmailService emailService,
        ICacheService cacheService,
        IOptions<PasswordResetOption> resetOption,
        ILogger<ForgotPasswordCommandHandler> logger,
        IMessageBus bus
    ) : ICommandHandler<ForgotPasswordCommand, Result<bool>>
    {
        private static readonly TimeSpan AccountCacheExpiration = TimeSpan.FromMinutes(5);

        /// <summary>Minimum delay between two reset emails for the same address.</summary>
        private static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(1);

        /// <summary>Entropy of the raw reset token (32 bytes = 256 bits).</summary>
        private const int TokenSizeInBytes = 32;

        /// <summary>
        /// Sends the reset link to the account's email address.
        /// </summary>
        /// <param name="command">The request containing the email address.</param>
        /// <param name="cancellationToken">Token used to cancel database and email operations.</param>
        /// <returns>True when the email was handed to the SMTP service, otherwise a failure result.</returns>
        public async Task<Result<bool>> HandleAsync(
            ForgotPasswordCommand command,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(command);

            var result = await HandleCoreAsync(command, cancellationToken);

            await bus.PublishAsync(new AuditEvent
            {
                ActorRole = "Public",
                Action = result.IsSuccess()
                    ? "Authentication.PasswordResetEmailSent"
                    : "Authentication.PasswordResetEmailFailed",
                EntityType = "Account",
                DetailsJson = System.Text.Json.JsonSerializer.Serialize(
                    new { Status = result.StatusCode }
                ),
            });

            return result;
        }

        /// <summary>
        /// Replaces any outstanding reset token with a fresh one and sends the reset
        /// link to the account's email address.
        /// </summary>
        /// <param name="command">The request containing the email address.</param>
        /// <param name="cancellationToken">Token used to cancel database and email operations.</param>
        /// <returns>True when the email was handed to the SMTP service, otherwise a failure result.</returns>
        private async Task<Result<bool>> HandleCoreAsync(
            ForgotPasswordCommand command,
            CancellationToken cancellationToken = default
        )
        {
            if (string.IsNullOrWhiteSpace(command.Email))
            {
                return Failure(ApplicationStatus.BadRequest, "A valid email address is required");
            }

            var email = command.Email.Trim().ToLowerInvariant();

            // Throttle resends so the endpoint cannot be abused to bomb a mailbox.
            var cooldownCacheKey = $"auth:account:reset:cooldown:{email}";
            var cooldown = await cacheService.GetAsync<string>(
                cooldownCacheKey,
                cancellationToken
            );

            if (cooldown is not null)
            {
                logger.LogInformation(
                    "Password reset email request for {Email} throttled by the resend cooldown",
                    email
                );
                return Failure(
                    ApplicationStatus.BusinessRuleViolation,
                    "A password reset email was sent recently, please wait before requesting another one"
                );
            }

            // Resolve the account cache-first, using the same key as the login/refresh flows.
            var accountCacheKey = $"auth:account:email:{email}";
            var account = await cacheService.GetAsync<Account>(accountCacheKey, cancellationToken);

            if (account is null)
            {
                account = await dbContext.Accounts.FirstOrDefaultAsync(
                    a => a.Email == email && a.Status == StatusType.Active && !a.IsDeleted,
                    cancellationToken
                );

                if (account is not null)
                {
                    await cacheService.SetAsync(
                        accountCacheKey,
                        account,
                        AccountCacheExpiration,
                        cancellationToken
                    );
                }
            }

            if (account is null)
            {
                logger.LogInformation(
                    "Password reset email request for {Email} ignored, no matching account",
                    email
                );
                return Failure(ApplicationStatus.BadRequest, "Account not found");
            }

            var option = resetOption.Value;
            var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(TokenSizeInBytes));
            var resetToken = new Token
            {
                HashToken = hashService.HashToken(rawToken),
                AccountId = account.Id,
                TokenType = TokenType.PasswordResetToken,
                ExpirationDate = DateTimeOffset.UtcNow.AddHours(option.TokenExpiresHours),
            };

            // Only the newest token stays live: whatever was issued before is invalidated.
            var previousTokens = await dbContext
                .Tokens.Where(t =>
                    t.AccountId == account.Id && t.TokenType == TokenType.PasswordResetToken
                )
                .ToListAsync(cancellationToken);

            dbContext.Tokens.RemoveRange(previousTokens);
            await dbContext.Tokens.AddAsync(resetToken, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            var link = BuildResetLink(option.ResetUrl, rawToken);

            try
            {
                await emailService.SendAsync(
                    account.Email,
                    "Reset your password",
                    BuildEmailBody(link, option.TokenExpiresHours),
                    cancellationToken
                );
            }
            catch (Exception exception)
            {
                // Compensate: the token was never delivered, so drop it instead of
                // leaving an unusable credential behind.
                logger.LogError(
                    exception,
                    "Failed to send the password reset email to {Email}, rolling back the issued token",
                    account.Email
                );

                dbContext.Tokens.Remove(resetToken);
                await dbContext.SaveChangesAsync(CancellationToken.None);

                return Failure(
                    ApplicationStatus.ThirdPartyServiceFailure,
                    "The password reset email could not be sent, please try again later"
                );
            }

            await cacheService.SetAsync(cooldownCacheKey, "1", ResendCooldown, cancellationToken);

            logger.LogInformation("Password reset email sent to {Email}", account.Email);
            return Result<bool>.Success(ApplicationStatus.Success, true);
        }

        /// <summary>Appends the raw token to the configured reset URL.</summary>
        private static string BuildResetLink(string resetUrl, string rawToken)
        {
            var separator = resetUrl.Contains('?') ? '&' : '?';
            return $"{resetUrl.TrimEnd('/')}{separator}token={Uri.EscapeDataString(rawToken)}";
        }

        /// <summary>Builds the HTML body of the password reset email.</summary>
        private static string BuildEmailBody(string link, int tokenExpiresHours)
        {
            return $"""
                <p>Hello,</p>
                <p>We received a request to reset your password. Open the link below to choose a new one. It expires in {tokenExpiresHours} hour(s).</p>
                <p><a href="{WebUtility.HtmlEncode(link)}">Reset my password</a></p>
                <p>If you did not request a password reset, you can safely ignore this email; your password stays unchanged.</p>
                """;
        }

        private static Result<bool> Failure(string statusCode, string description)
        {
            return Result<bool>.Failure(new ApplicationError(statusCode, description));
        }
    }
}
