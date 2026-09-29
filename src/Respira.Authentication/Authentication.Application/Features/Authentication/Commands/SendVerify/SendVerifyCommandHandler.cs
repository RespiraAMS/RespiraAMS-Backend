using System.Net;
using System.Security.Cryptography;
using Authentication.Application.Constracts.Authentication;
using Authentication.Application.Constracts.Cache;
using Authentication.Application.Constracts.Data;
using Authentication.Application.Constracts.Email;
using Authentication.Domain.Entities;
using Authentication.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Authentication.Application.Features.Authentication.Commands.SendVerify
{
    /// <summary>
    /// Issues a single-use email verification token for an account and emails a
    /// confirmation link to its address. Only the SHA-256 hash of the token is
    /// persisted; the raw token exists only in the email that is sent out.
    /// </summary>
    public class SendVerifyCommandHandler(
        IAuthDbContext dbContext,
        IHashService hashService,
        IEmailService emailService,
        ICacheService cacheService,
        IOptions<EmailVerificationOption> verificationOption,
        ILogger<SendVerifyCommandHandler> logger
    ) : ICommandHandler<SendVerifyCommand, Result<bool>>
    {
        private static readonly TimeSpan AccountCacheExpiration = TimeSpan.FromMinutes(5);

        /// <summary>Minimum delay between two verification emails for the same address.</summary>
        private static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(1);

        /// <summary>Entropy of the raw verification token (32 bytes = 256 bits).</summary>
        private const int TokenSizeInBytes = 32;

        /// <summary>
        /// Replaces any outstanding verification token with a fresh one and sends the
        /// confirmation link to the account's email address.
        /// </summary>
        /// <param name="command">The verification request containing the email address.</param>
        /// <param name="cancellationToken">Token used to cancel database and email operations.</param>
        /// <returns>True when the email was handed to the SMTP service, otherwise a failure result.</returns>
        public async Task<Result<bool>> HandleAsync(
            SendVerifyCommand command,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(command);

            if (string.IsNullOrWhiteSpace(command.Email))
            {
                return Failure(ApplicationStatus.BadRequest, "A valid email address is required");
            }

            var email = command.Email.Trim().ToLowerInvariant();

            // Throttle resends so the endpoint cannot be abused to bomb a mailbox.
            var cooldownCacheKey = $"auth:account:verify:cooldown:{email}";
            var cooldown = await cacheService.GetAsync<string>(
                cooldownCacheKey,
                cancellationToken
            );

            if (cooldown is not null)
            {
                logger.LogInformation(
                    "Verification email request for {Email} throttled by the resend cooldown",
                    email
                );
                return Failure(
                    ApplicationStatus.BusinessRuleViolation,
                    "A verification email was sent recently, please wait before requesting another one"
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
                    "Verification email request for {Email} ignored, no matching account",
                    email
                );
                return Failure(ApplicationStatus.BadRequest, "Account not found");
            }

            if (account.IsEmailConfirmed)
            {
                logger.LogInformation(
                    "Verification email request for {Email} rejected, the address is already confirmed",
                    email
                );
                return Failure(
                    ApplicationStatus.BusinessRuleViolation,
                    "The email address is already verified"
                );
            }

            var option = verificationOption.Value;
            var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(TokenSizeInBytes));
            var verificationToken = new Token
            {
                HashToken = hashService.HashToken(rawToken),
                AccountId = account.Id,
                TokenType = TokenType.EmailVerificationToken,
                ExpirationDate = DateTimeOffset.UtcNow.AddHours(option.TokenExpiresHours),
            };

            // Only the newest token stays live: whatever was issued before is invalidated.
            var previousTokens = await dbContext
                .Tokens.Where(t =>
                    t.AccountId == account.Id
                    && t.TokenType == TokenType.EmailVerificationToken
                )
                .ToListAsync(cancellationToken);

            dbContext.Tokens.RemoveRange(previousTokens);
            await dbContext.Tokens.AddAsync(verificationToken, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            var link = BuildConfirmLink(option.ConfirmUrl, rawToken);

            try
            {
                await emailService.SendAsync(
                    account.Email,
                    "Verify your email address",
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
                    "Failed to send the verification email to {Email}, rolling back the issued token",
                    account.Email
                );

                dbContext.Tokens.Remove(verificationToken);
                await dbContext.SaveChangesAsync(CancellationToken.None);

                return Failure(
                    ApplicationStatus.ThirdPartyServiceFailure,
                    "The verification email could not be sent, please try again later"
                );
            }

            await cacheService.SetAsync(cooldownCacheKey, "1", ResendCooldown, cancellationToken);

            logger.LogInformation("Verification email sent to {Email}", account.Email);
            return Result<bool>.Success(ApplicationStatus.Success, true);
        }

        /// <summary>Appends the raw token to the configured confirmation URL.</summary>
        private static string BuildConfirmLink(string confirmUrl, string rawToken)
        {
            var separator = confirmUrl.Contains('?') ? '&' : '?';
            return $"{confirmUrl.TrimEnd('/')}{separator}token={Uri.EscapeDataString(rawToken)}";
        }

        /// <summary>Builds the HTML body of the verification email.</summary>
        private static string BuildEmailBody(string link, int tokenExpiresHours)
        {
            return $"""
                <p>Hello,</p>
                <p>Please confirm your email address by opening the link below. It expires in {tokenExpiresHours} hour(s).</p>
                <p><a href="{WebUtility.HtmlEncode(link)}">Verify my email</a></p>
                <p>If you did not request this email, you can safely ignore it.</p>
                """;
        }

        private static Result<bool> Failure(string statusCode, string description)
        {
            return Result<bool>.Failure(new Error(statusCode, description));
        }
    }
}
