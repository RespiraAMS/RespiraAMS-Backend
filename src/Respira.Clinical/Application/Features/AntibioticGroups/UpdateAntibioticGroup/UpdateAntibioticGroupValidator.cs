namespace Application.Features.AntibioticGroups.UpdateAntibioticGroup;

public class UpdateAntibioticGroupValidator : AbstractValidator<UpdateAntibioticGroupCommand>
{
    public UpdateAntibioticGroupValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Antibiotic group ID is required.");
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
