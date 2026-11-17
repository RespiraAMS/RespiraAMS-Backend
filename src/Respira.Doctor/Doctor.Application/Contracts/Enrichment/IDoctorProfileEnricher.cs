using Respira.Doctor.Application.Features.Doctors.Common;

namespace Respira.Doctor.Application.Contracts.Enrichment
{
    /// <summary>
    /// Fills the account and media fields of doctor profiles by querying the owning
    /// microservices over the message broker.
    /// </summary>
    public interface IDoctorProfileEnricher
    {
        Task EnrichAsync(
            IEnumerable<DoctorProfileItem> profiles,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Resolves the accessible URL of a media asset, for single-profile queries
        /// that do not use <see cref="DoctorProfileItem"/>.
        /// </summary>
        /// <returns>The media URL, or <c>null</c> when the asset cannot be resolved.</returns>
        Task<string?> ResolveAvatarUrlAsync(
            Guid? mediaId,
            CancellationToken cancellationToken = default
        );
    }
}
