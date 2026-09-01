using Respira.ServiceDefaults.Contracts.CQRS;

namespace Application.Features.Authentication.VerifyEmail
{
    /// <summary>
    /// Command to verify a doctor's email using a verification token.
    /// </summary>
    public record VerifyEmailCommand : ICommand
    {
        /// <summary>Raw verification token (hashed before lookup)</summary>
        public required string Token { get; init; }

        /// <summary>Email address to confirm</summary>
        public required string Email { get; init; }
    }
}
