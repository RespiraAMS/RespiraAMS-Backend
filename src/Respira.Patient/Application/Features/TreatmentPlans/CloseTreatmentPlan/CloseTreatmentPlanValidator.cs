using FluentValidation;

namespace Respira.Patient.Application.Features.TreatmentPlans.CloseTreatmentPlan
{
    public class CloseTreatmentPlanValidator : AbstractValidator<CloseTreatmentPlanCommand>
    {
        public CloseTreatmentPlanValidator()
        {
            RuleFor(x => x.TreatmentPlanId).NotEmpty().WithMessage("Treatment plan ID is required");
            RuleFor(x => x.Reason).NotEmpty().WithMessage("Reason for closing treatment plan is required");
        }
    }
}
