using Respira.ServiceDefaults.Contracts.CQRS;

namespace Authentication.Application.Features.Authentication.Commands.VerifyEmail
{
    /// <summary>
    /// Confirms an account's email address using the raw token carried by the
    /// verification link that was emailed to it.
    /// </summary>
    public record VerifyEmailCommand : ICommand
    {
        /// <summary>
        /// The raw verification token embedded in the confirmation link. It is only
        /// ever compared against its stored SHA-256 hash, never persisted again.
        /// </summary>
        public required string Token { get; init; }
    }
}
