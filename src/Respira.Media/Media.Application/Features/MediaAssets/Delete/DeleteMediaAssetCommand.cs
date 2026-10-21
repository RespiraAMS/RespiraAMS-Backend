using Respira.ServiceDefaults.Contracts.CQRS;

namespace Media.Application.Features.MediaAssets.Delete
{
    /// <summary>
    /// Removes a media asset: the object in R2 first, then its database record. Used by
    /// the delete saga and by the avatar cleanup of the update saga.
    /// </summary>
    public record DeleteMediaAssetCommand : ICommand
    {
        /// <summary>Identifier of the media asset to remove.</summary>
        public required Guid MediaId { get; init; }
    }
}
