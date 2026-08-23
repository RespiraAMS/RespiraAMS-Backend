namespace Application.Abstracts.Storage;

/// <summary>
/// Abstraction over a binary object storage backend (e.g. Cloudflare R2).
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Uploads a file and returns its storage location.
    /// </summary>
    Task<StorageResult> UploadAsync(
        string fileName,
        string contentType,
        byte[] data,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Uploads a file from a stream and returns its storage location. Prefer this overload
    /// for HTTP file uploads so the payload is streamed instead of buffered into memory.
    /// </summary>
    Task<StorageResult> UploadAsync(
        string fileName,
        string contentType,
        Stream stream,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Deletes a previously uploaded object by its key.
    /// </summary>
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);
}
