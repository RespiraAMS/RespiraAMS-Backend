using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Authentication.SendEmailVerification;

/// <summary>
/// Command to (re)send the email verification link for a registered doctor.
/// Generates, persists and emails a fresh verification token.
/// </summary>
public record RequestEmailVerificationCommand : ICommand
{
    /// <summary>Email of the account to verify</summary>
    public required string Email { get; init; }
}
