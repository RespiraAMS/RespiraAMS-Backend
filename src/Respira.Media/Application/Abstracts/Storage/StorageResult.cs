namespace Application.Abstracts.Storage;

/// <summary>
/// Result of a successful object upload to storage.
/// </summary>
public record StorageResult
{
    /// <summary>Unique key identifying the object in the bucket</summary>
    public required string ObjectKey { get; init; }

    /// <summary>Public or internal URL to access the object</summary>
    public required string Url { get; init; }

    /// <summary>Name of the storage bucket</summary>
    public required string BucketName { get; init; }
}
