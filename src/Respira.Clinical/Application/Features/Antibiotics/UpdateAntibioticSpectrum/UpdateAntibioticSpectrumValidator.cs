namespace Application.Features.Antibiotics.UpdateAntibioticSpectrum;

public class UpdateAntibioticSpectrumValidator : AbstractValidator<UpdateAntibioticSpectrumCommand>
{
    public UpdateAntibioticSpectrumValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Antibiotic ID is required");
        RuleFor(x => x.PathogenIds)
            .NotEmpty()
            .WithMessage("List of pathogen ID is required (not empty)");
        RuleForEach(x => x.PathogenIds)
            .NotEmpty()
            .WithMessage("Pathogen ID is required");
    }
}