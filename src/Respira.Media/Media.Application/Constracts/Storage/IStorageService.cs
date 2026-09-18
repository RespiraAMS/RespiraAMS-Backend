namespace Media.Application.Constracts.Storage
{
    public interface IStorageService
    {
        /// <summary>
        /// Uploads a file to Cloudflare R2 and returns the object key.
        /// </summary>
        Task<string> UploadAsync(
            UploadRequest request,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Deletes an object from Cloudflare R2 by its key.
        /// </summary>
        Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a pre-signed URL for accessing the object.
        /// </summary>
        Task<string> GetPresignedUrlAsync(
            string objectKey,
            TimeSpan? expiration = null,
            CancellationToken cancellationToken = default
        );
    }
}
