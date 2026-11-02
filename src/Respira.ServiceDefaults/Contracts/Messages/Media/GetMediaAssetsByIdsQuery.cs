using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.ServiceDefaults.Contracts.Messages.Media
{
    /// <summary>
    /// Query sent by other microservices to resolve media asset details by identifier.
    /// </summary>
    public record GetMediaAssetsByIdsQuery : IQuery
    {
        /// <summary>Identifiers of the media assets to resolve.</summary>
        public Guid[] MediaIds { get; init; } = [];
    }

    /// <summary>
    /// Media asset data shared between microservices.
    /// </summary>
    public record MediaAssetSummary
    {
        /// <summary>Media asset identifier.</summary>
        public Guid Id { get; init; }

        /// <summary>Accessible URL of the stored object, if available.</summary>
        public string? Url { get; init; }
    }
}
