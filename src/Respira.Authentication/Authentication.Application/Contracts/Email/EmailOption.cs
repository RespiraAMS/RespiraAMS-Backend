using System.ComponentModel.DataAnnotations;

namespace Respira.Authentication.Application.Contracts.Email;

public sealed class EmailOption
{
    public const string SectionName = "Email";

    /// <summary>Host of the SMTP server the emails are sent through.</summary>
    [Required]
    public required string Host { get; init; }

    /// <summary>Port of the SMTP server.</summary>
    [Range(1, 65535)]
    public required int Port { get; init; }

    /// <summary>User the SMTP server authenticates with.</summary>
    [Required]
    public required string UserName { get; init; }

    /// <summary>Password the SMTP server authenticates with.</summary>
    [Required]
    public required string Password { get; init; }

    /// <summary>Address the emails are sent from.</summary>
    [Required]
    public required string FromEmail { get; init; }

    /// <summary>Name shown as the sender of the emails.</summary>
    [Required]
    public required string FromName { get; init; }

    /// <summary>Whether the connection uses SSL right from the start.</summary>
    public bool UseSsl { get; init; } = true;
}
