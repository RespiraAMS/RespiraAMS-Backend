using Respira.Patient.Application.Features.TreatmentPlans.CloseTreatmentPlan;
using Respira.Patient.Application.Features.TreatmentPlans.ReviewTreatmentPlan;
using Respira.Patient.Domain.Enums;

namespace Respira.Patient.API.Dtos
{
    public record CloseTreatmentPlanRequestDto
    {
        public required string Reason { get; set; }

        public CloseTreatmentPlanCommand ToCommand(Guid id)
        {
            return new CloseTreatmentPlanCommand
            {
                TreatmentPlanId = id,
                Reason = Reason
            };
        }
    }

    public record ReviewTreatmentPlanRequestDto
    {
        public required AdherenceState AdherenceState { get; set; }

        public ReviewTreatmentPlanCommand ToCommand(Guid id)
        {
            return new ReviewTreatmentPlanCommand
            {
                TreatmentPlanId = id,
                AdherenceState = AdherenceState
            };
        }
    }
}
