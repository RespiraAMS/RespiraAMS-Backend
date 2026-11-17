using System.ComponentModel.DataAnnotations;

namespace Respira.Media.Application.Contracts.Storage;

public sealed class R2Options
{
    public const string SectionName = "R2";

    /// <summary>Endpoint of the Cloudflare R2 storage.</summary>
    [Required]
    public required string Endpoint { get; init; }

    /// <summary>Access key of the Cloudflare R2 storage.</summary>
    [Required]
    public required string AccessKey { get; init; }

    /// <summary>Secret key of the Cloudflare R2 storage.</summary>
    [Required]
    public required string SecretKey { get; init; }

    /// <summary>Bucket the media assets are stored in.</summary>
    [Required]
    public required string BucketName { get; init; }

    /// <summary>Public address the stored assets are served from.</summary>
    [Required]
    public required string PublicUrl { get; init; }
}
