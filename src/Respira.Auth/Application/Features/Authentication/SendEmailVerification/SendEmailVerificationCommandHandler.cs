using Application.Abstracts.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Authentication.SendEmailVerification
{
    /// <summary>
    /// Sends the email verification email containing a clickable link built from configuration
    /// </summary>
    /// <param name="logger">Logger</param>
    /// <param name="emailService">Email sending service</param>
    /// <param name="verifyEmailOption">Verification link configuration</param>
    public class SendEmailVerificationCommandHandler(
        ILogger<SendEmailVerificationCommandHandler> logger,
        ISendEmailService emailService,
        IOptions<VerifyEmailOption> verifyEmailOption
    ) : ICommandHandler<SendEmaiLVerificationCommand, bool>
    {
        /// <summary>
        /// Builds the verification link ({token}/{email} placeholders are URL-escaped and
        /// replaced) and sends the email
        /// </summary>
        /// <param name="command">Recipient email and verification token</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the email was sent, false if sending failed</returns>
        public async Task<bool> HandleAsync(
            SendEmaiLVerificationCommand command,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var link = verifyEmailOption
                    .Value.LinkTemplate.Replace("{token}", Uri.EscapeDataString(command.Token))
                    .Replace("{email}", Uri.EscapeDataString(command.Email));

                const string subject = "Verify your email";
                var body = $"""
                    <p>Hi,</p>
                    <p>Click the link below to verify your email address:</p>
                    <p><a href="{link}">Verify email</a></p>
                    <p>If you did not request this, you can safely ignore this email.</p>
                    """;

                await emailService.SendEmailAsync(command.Email, subject, body);
                return true;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to send verification email to {Email}", command.Email);
                return false;
            }
        }
    }
}
