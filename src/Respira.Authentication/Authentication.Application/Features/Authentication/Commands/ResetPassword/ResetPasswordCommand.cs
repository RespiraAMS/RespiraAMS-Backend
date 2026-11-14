using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Authentication.Application.Features.Authentication.Commands.ResetPassword;

/// <summary>
/// Sets a new password using the token carried by the reset link.
/// </summary>
public record ResetPasswordCommand : ICommand
{
    /// <summary>The raw token received through the reset link.</summary>
    public required string Token { get; set; }

    /// <summary>The new password.</summary>
    public required string NewPassword { get; set; }

    /// <summary>The new password typed a second time by the user.</summary>
    public required string ConfirmPassword { get; set; }
}
