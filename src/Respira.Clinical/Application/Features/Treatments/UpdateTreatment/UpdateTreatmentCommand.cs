using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.Treatments.UpdateTreatment
{
    public record UpdateTreatmentCommand : ICommand
    {
        /// <summary>
        /// Treatment ID
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// Severity of the treatment
        /// </summary>
        public required Severity Severity { get; set; }

        /// <summary>
        /// Treatment site of the treatment
        /// </summary>
        public required TreatmentSite TreatmentSite { get; set; }

        /// <summary>
        /// List of medicine compositions. Each sub-list represents a medicine composition,
        /// where it contains one or multiple antibiotic IDs
        /// </summary>
        public List<List<Guid>> Medicines { get; set; } = [];

        /// <summary>
        /// List of pathogen IDs associated with the treatment
        /// </summary>
        public List<Guid> Pathogens { get; set; } = [];

        /// <summary>
        /// List of criteria IDs associated with the treatment
        /// </summary>
        public List<Guid> Criteria { get; set; } = [];
    }
}
