namespace Application.Features.Antibiotics.UpdateAntibiotic;

public class UpdateAntibioticValidator : AbstractValidator<UpdateAntibioticCommand>
{
    public UpdateAntibioticValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Antibiotic ID is required");
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Antibiotic name is required");
        RuleFor(x => x.AntibioticGroupId)
            .NotEmpty()
            .WithMessage("Antibiotic group ID is required");
        RuleFor(x => x.Classification)
            .IsInEnum()
            .WithMessage("Antibiotic category is required");
    }
}
