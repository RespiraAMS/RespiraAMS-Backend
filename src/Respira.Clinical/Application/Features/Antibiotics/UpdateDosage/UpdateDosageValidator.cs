using Application.Features.Shared.ManageRange;

namespace Application.Features.Antibiotics.UpdateDosage;

public class UpdateDosageValidator : AbstractValidator<UpdateDosageCommand>
{
    public UpdateDosageValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Dosage ID is required");
        RuleFor(x => x.RouteOfAdministration)
            .IsInEnum()
            .WithMessage("Route of administration is invalid");
        RuleFor(x => x.Dose)
            .NotEmpty()
            .WithMessage("Dose is required");
        RuleFor(x => x.GlomerularFiltrationRate)
            .SetValidator(new RangeValidator()!)
            .When(x => x.GlomerularFiltrationRate != null);
    }
}
