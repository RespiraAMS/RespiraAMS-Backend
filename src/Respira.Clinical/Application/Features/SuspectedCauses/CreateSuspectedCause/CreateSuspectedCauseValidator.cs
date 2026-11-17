using FluentValidation;

namespace Respira.Clinical.Application.Features.SuspectedCauses.CreateSuspectedCause
{
    public class CreateSuspectedCauseValidator : AbstractValidator<CreateSuspectedCauseCommand>
    {
        public CreateSuspectedCauseValidator()
        {
            RuleFor(x => x.PathogenId)
                .NotEmpty()
                .WithMessage("Pathogen ID is required");
            RuleFor(x => x.Severity)
                .IsInEnum()
                .WithMessage("Invalid value for severity");
            RuleFor(x => x.TreatmentSite)
                .IsInEnum()
                .WithMessage("Invalid value for treatment site");
        }
    }
}
