using FluentValidation;
using Respira.Patient.Domain.Enums;

namespace Respira.Patient.Application.Features.TreatmentPlans.ReviewTreatmentPlan
{
    public class ReviewTreatmentPlanValidator : AbstractValidator<ReviewTreatmentPlanCommand>
    {
        public ReviewTreatmentPlanValidator()
        {
            RuleFor(x => x.TreatmentPlanId).NotEmpty().WithMessage("Treatment plan ID is required");
            RuleFor(x => x.AdherenceState)
                .Must(x => x == AdherenceState.NotAdhereInvalid || x == AdherenceState.NotAdhereValid)
                .WithMessage("Review treatment plan can only accept NotAdhereValid or NotAdhereInvalid");
        }
    }
}
