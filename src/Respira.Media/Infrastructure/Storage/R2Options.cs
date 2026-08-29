namespace Infrastructure.Storage;

/// <summary>
/// Cloudflare R2 storage configuration. Bound from the "R2" section.
/// </summary>
public class R2Options
{
    public const string SectionName = "R2";

    /// <summary>S3-compatible endpoint URL</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>R2 access key ID</summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>R2 secret access key</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>R2 bucket name</summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>Public base URL for object access (custom domain or r2.dev)</summary>
    public string PublicBaseUrl { get; set; } = string.Empty;
}
