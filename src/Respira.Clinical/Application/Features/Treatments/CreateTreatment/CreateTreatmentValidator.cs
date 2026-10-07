using FluentValidation;

namespace Respira.Clinical.Application.Features.Treatments.CreateTreatment
{
    public class CreateTreatmentValidator : AbstractValidator<CreateTreatmentCommand>
    {
        public CreateTreatmentValidator()
        {
            RuleFor(x => x.Severity)
                .IsInEnum()
                .WithMessage("Invalid value for severity");
            RuleFor(x => x.TreatmentSite)
                .IsInEnum()
                .WithMessage("Invalid value for treatment site");
            RuleFor(x => x.Medicines)
                .Must(x => x.Count > 0)
                .WithMessage("No medicine compositions given");
            RuleForEach(x => x.Medicines)
                .Must(x => x.Count > 0)
                .WithMessage("Medicine composition must not empty");
            RuleForEach(x => x.Medicines)
                .Must(composition => composition.All(id => id != Guid.Empty))
                .WithMessage("Antibiotic ID must be a valid UUID");
            RuleForEach(x => x.Pathogens)
                .NotEmpty()
                .When(x => x.Pathogens.Count > 0)
                .WithMessage("Pathogen ID must be valid UUID");
            RuleForEach(x => x.Criteria)
                .NotEmpty()
                .When(x => x.Criteria.Count > 0)
                .WithMessage("Criteria ID must be valid UUID");
        }
    }
}
