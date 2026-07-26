namespace Application.Features.Antibiotics.DeleteDosage;

public class DeleteDosageValidator : AbstractValidator<DeleteDosageCommand>
{
    public DeleteDosageValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Dosage ID is required");
    }
}