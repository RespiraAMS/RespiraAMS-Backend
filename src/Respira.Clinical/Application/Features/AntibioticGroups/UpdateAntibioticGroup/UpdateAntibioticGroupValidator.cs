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
        RuleFor(x => x.ParentId)
            .NotEmpty()
            .WithMessage("Antibiotic group parent ID is required.")
            .When(x => x.ParentId != null);
    }
}