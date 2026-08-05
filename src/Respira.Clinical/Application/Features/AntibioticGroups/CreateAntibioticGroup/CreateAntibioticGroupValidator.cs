namespace Application.Features.AntibioticGroups.CreateAntibioticGroup;

public class CreateAntibioticGroupValidator : AbstractValidator<CreateAntibioticGroupCommand>
{
    public CreateAntibioticGroupValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Antibiotic group name is required.");
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Antibiotic group description is required.");
        RuleFor(x => x.ParentId)
            .NotEmpty()
            .WithMessage("Antibiotic group parent ID is required.")
            .When(x => x.ParentId != null);
    }
}