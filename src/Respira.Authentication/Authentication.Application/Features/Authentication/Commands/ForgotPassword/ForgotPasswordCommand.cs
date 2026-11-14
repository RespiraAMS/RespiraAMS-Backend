using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Authentication.Application.Features.Authentication.Commands.ForgotPassword;

/// <summary>
/// Requests a password reset link for an account. The link is delivered by email,
/// so possession of the address is what authorizes the password change.
/// </summary>
public record ForgotPasswordCommand : ICommand
{
    /// <summary>Email address of the account that wants to reset its password.</summary>
    public required string Email { get; set; }
}
