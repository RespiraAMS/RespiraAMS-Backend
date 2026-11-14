using System.ComponentModel.DataAnnotations;

namespace Respira.Authentication.Application.Contracts.Email;

/// <summary>
/// Configuration for the reset link carried by the password reset message.
/// </summary>
public sealed class PasswordResetOption
{
    public const string SectionName = "PasswordReset";

    /// <summary>
    /// Frontend URL the reset link points at. The raw token is appended to it
    /// as a <c>token</c> query parameter (a <c>&amp;</c> separator is used when the
    /// URL already carries a query string).
    /// </summary>
    [Required]
    public required string ResetUrl { get; init; }

    /// <summary>Lifetime of the issued password reset token, in hours.</summary>
    [Range(1, 72)]
    public int TokenExpiresHours { get; init; } = 1;
}
