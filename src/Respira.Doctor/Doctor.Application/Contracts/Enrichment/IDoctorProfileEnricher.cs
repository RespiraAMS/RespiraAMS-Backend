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
    }
}
