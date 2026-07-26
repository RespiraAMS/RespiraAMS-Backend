namespace Application.Features.Antibiotics.DeleteAntibiotic;

public class DeleteAntibioticValidator : AbstractValidator<DeleteAntibioticCommand>
{
    public DeleteAntibioticValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Antibiotic ID is required");
    }
}