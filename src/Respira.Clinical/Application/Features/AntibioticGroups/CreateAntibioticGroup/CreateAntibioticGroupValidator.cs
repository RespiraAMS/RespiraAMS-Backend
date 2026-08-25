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
        // NotEmpty check in Fluent validation check both null and empty case
        RuleFor(x => x.ParentId)
            .Must(x => x == null || x != Guid.Empty)
            .WithMessage("Antibiotic group must either be null or non empty UUID");
    }
}
