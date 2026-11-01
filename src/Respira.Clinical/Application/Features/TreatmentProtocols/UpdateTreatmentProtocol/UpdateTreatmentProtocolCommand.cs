using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.TreatmentProtocols.UpdateTreatmentProtocol
{
    public record UpdateTreatmentProtocolCommand : ICommand
    {
        /// <summary>
        /// TreatmentProtocol ID
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// Severity of the treatment
        /// </summary>
        public required Severity Severity { get; set; }

        /// <summary>
        /// TreatmentProtocol site of the treatment
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

        /// <summary>
        /// Version of the metric
        /// </summary>
        public required string Version { get; set; }

        /// <summary>
        /// Metric publisher
        /// </summary>
        public required string Publisher { get; set; }

        /// <summary>
        /// Metric status, is applied or not
        /// </summary>
        public required bool IsApplied { get; set; }
    }
}
