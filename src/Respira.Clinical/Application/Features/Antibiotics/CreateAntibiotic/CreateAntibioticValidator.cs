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
        RuleFor(x => x.Classification)
            .IsInEnum()
            .WithMessage("Antibiotic category is required");
        RuleFor(x => x.RouteOfAdministration)
            .IsInEnum()
            .WithMessage("Antibiotic standard dose's route of administration is required");
        RuleFor(x => x.StandardDose)
            .NotEmpty()
            .WithMessage("Antibiotic's standard dose is required");
    }
}
