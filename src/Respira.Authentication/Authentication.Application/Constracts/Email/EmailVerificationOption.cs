using System.ComponentModel.DataAnnotations;

namespace Authentication.Application.Constracts.Email;

/// <summary>
/// Configuration for the confirmation link carried by the email verification message.
/// </summary>
public sealed class EmailVerificationOption
{
    public const string SectionName = "EmailVerification";

    /// <summary>
    /// Frontend URL the verification link points at. The raw token is appended to it
    /// as a <c>token</c> query parameter (a <c>&amp;</c> separator is used when the
    /// URL already carries a query string).
    /// </summary>
    [Required]
    public required string ConfirmUrl { get; init; }

    /// <summary>Lifetime of the issued verification token, in hours.</summary>
    [Range(1, 720)]
    public int TokenExpiresHours { get; init; } = 24;
}
