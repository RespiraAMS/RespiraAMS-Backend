namespace Infrastructure.Storage;

/// <summary>
/// Configuration for the Cloudflare R2 storage backend. Bound from the "R2" configuration section.
/// </summary>
public class R2Options
{
    public const string SectionName = "R2";

    /// <summary>S3-compatible endpoint, e.g. https://&lt;accountId&gt;.r2.cloudflarestorage.com</summary>
    public string Endpoint { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string BucketName { get; set; } = string.Empty;

    /// <summary>Public base URL used to build object URLs (custom domain or r2.dev).</summary>
    public string PublicBaseUrl { get; set; } = string.Empty;
}
