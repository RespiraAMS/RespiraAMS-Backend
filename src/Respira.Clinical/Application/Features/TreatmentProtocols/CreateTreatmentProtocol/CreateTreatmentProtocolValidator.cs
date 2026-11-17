using FluentValidation;

namespace Respira.Clinical.Application.Features.TreatmentProtocols.CreateTreatmentProtocol
{
    public class CreateTreatmentProtocolValidator : AbstractValidator<CreateTreatmentProtocolCommand>
    {
        public CreateTreatmentProtocolValidator()
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
            RuleFor(x => x.Version).NotEmpty().WithMessage("Treatment protocol version is required");
            RuleFor(x => x.Publisher).NotEmpty().WithMessage("Treatment protocol publisher is required");
        }
    }
}
