using Respira.ServiceDefaults.Contracts.CQRS;

namespace Application.Features.Authentication.SendEmailVerification
{
    /// <summary>
    /// Command to send the email verification link to a doctor.
    /// </summary>
    public record SendEmaiLVerificationCommand : ICommand
    {
        /// <summary>Recipient email address</summary>
        public required string Email { get; init; }

        /// <summary>Verification token to embed in the link</summary>
        public required string Token { get; init; }
    }
}
