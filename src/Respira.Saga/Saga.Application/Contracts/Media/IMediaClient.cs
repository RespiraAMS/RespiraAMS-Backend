using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Saga.Application.Constracts.Media
{
    /// <summary>
    /// Thin HTTP wrapper over the Media microservice. The avatar is a file stream, which
    /// cannot travel over the message broker, so this single step of the saga is a
    /// synchronous call while every other step is driven by commands and events.
    /// </summary>
    public interface IMediaClient
    {
        /// <summary>
        /// Uploads an image to R2 storage through the Media API and returns the identifier
        /// of the created media asset.
        /// </summary>
        /// <param name="content">Seekable stream of the image to upload.</param>
        /// <param name="fileName">Original file name, used when the media asset is named.</param>
        /// <param name="folder">Folder/prefix inside the R2 bucket.</param>
        /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
        /// <returns>The media asset identifier, or a failure describing why the upload failed.</returns>
        Task<Result<Guid>> UploadAvatarAsync(
            Stream content,
            string fileName,
            string folder,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Deletes a media asset through the Media API (<c>DELETE /api/v1/media-assets/{id}</c>).
        /// Deleting an asset that no longer exists succeeds, so the call is safe to retry.
        /// </summary>
        /// <param name="mediaId">Identifier of the media asset to remove.</param>
        /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
        /// <returns>The identifier of the deleted asset, or a failure describing why the delete failed.</returns>
        Task<Result<Guid>> DeleteAssetAsync(Guid mediaId, CancellationToken cancellationToken = default);
    }
}
