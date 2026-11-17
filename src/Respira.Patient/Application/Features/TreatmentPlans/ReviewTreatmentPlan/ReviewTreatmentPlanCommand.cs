using Respira.Patient.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Patient.Application.Features.TreatmentPlans.ReviewTreatmentPlan
{
    public record ReviewTreatmentPlanCommand : ICommand
    {
        public required Guid TreatmentPlanId { get; set; }
        public required AdherenceState AdherenceState { get; set; }
    }
}
