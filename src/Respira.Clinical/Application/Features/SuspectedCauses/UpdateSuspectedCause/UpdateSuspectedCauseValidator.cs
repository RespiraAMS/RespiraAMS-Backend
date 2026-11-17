using FluentValidation;

namespace Respira.Clinical.Application.Features.SuspectedCauses.UpdateSuspectedCause
{
    public class UpdateSuspectedCauseValidator : AbstractValidator<UpdateSuspectedCauseCommand>
    {
        public UpdateSuspectedCauseValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("ID is required");
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
