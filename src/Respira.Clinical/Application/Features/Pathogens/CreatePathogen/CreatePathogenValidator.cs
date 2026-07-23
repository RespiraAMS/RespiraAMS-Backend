namespace Application.Features.Pathogens.CreatePathogen;

public class CreatePathogenValidator : AbstractValidator<CreatePathogenCommand>
{
    public CreatePathogenValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Pathogen name is required");
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Pathogen description is required");
    }
}