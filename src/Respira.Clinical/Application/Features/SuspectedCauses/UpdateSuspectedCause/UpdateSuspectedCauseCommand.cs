using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.SuspectedCauses.UpdateSuspectedCause
{
    public record UpdateSuspectedCauseCommand : ICommand
    {
        /// <summary>
        /// ID of suspected cause to update
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// Pathogen ID
        /// </summary>
        public required Guid PathogenId { get; set; }

        /// <summary>
        /// Severity caused by this pathogen
        /// </summary>
        public required Severity Severity { get; set; }

        /// <summary>
        /// Treatment site assigned to patient when catching the disease with this pathogen
        /// </summary>
        public required TreatmentSite TreatmentSite { get; set; }
    }
}
