namespace Application.Abstracts.Storage;

/// <summary>
/// Result of a successful object upload.
/// </summary>
public record StorageResult
{
    public required string ObjectKey { get; init; }
    public required string Url { get; init; }
    public required string BucketName { get; init; }
}
