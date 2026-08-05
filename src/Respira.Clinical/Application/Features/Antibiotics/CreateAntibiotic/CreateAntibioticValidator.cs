namespace Application.Features.Antibiotics.CreateAntibiotic;

public class CreateAntibioticValidator : AbstractValidator<CreateAntibioticCommand>
{
    public CreateAntibioticValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Antibiotic name is required");
        RuleFor(x => x.AntibioticGroupId)
            .NotEmpty()
            .WithMessage("Antibiotic group ID is required");
        RuleFor(x => x.Category)
            .IsInEnum()
            .WithMessage("Antibiotic category is required");
    }
}