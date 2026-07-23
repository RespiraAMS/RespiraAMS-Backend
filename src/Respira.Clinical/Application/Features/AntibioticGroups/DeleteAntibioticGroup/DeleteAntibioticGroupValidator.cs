namespace Application.Features.AntibioticGroups.DeleteAntibioticGroup;

public class DeleteAntibioticGroupValidator : AbstractValidator<DeleteAntibioticGroupCommand>
{
    public DeleteAntibioticGroupValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Antibiotic group ID is required");
    }
}