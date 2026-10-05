using FluentValidation;
using Respira.Clinical.Application.Features.Shared.ManageRange;

namespace Respira.Clinical.Application.Features.Antibiotics.AddDosage
{
    public class AddDosageValidator : AbstractValidator<AddDosageCommand>
    {
        public AddDosageValidator()
        {
            RuleFor(x => x.AntibioticId)
                .NotEmpty()
                .WithMessage("Antibiotic ID is required");
            RuleFor(x => x.RouteOfAdministration)
                .IsInEnum()
                .WithMessage("Route of administration is invalid");
            RuleFor(x => x.Dose)
                .NotEmpty()
                .WithMessage("Dose is required");
            RuleFor(x => x.Crcl)
                .SetValidator(new RangeValidator()!)
                .When(x => x.Crcl != null);
        }
    }
}
