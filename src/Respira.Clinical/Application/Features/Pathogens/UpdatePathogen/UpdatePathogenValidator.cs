namespace Application.Features.Pathogens.UpdatePathogen;

public class UpdatePathogenValidator : AbstractValidator<UpdatePathogenCommand>
{
    public UpdatePathogenValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Pathogen ID is required");
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Pathogen name is required");
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Pathogen description is required");
    }
}