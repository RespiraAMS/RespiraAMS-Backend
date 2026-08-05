namespace Application.Features.Antibiograms.DeleteAntibiogram;

public class DeleteAntibiogramValidator : AbstractValidator<DeleteAntibiogramCommand>
{
    public DeleteAntibiogramValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Antibiogram ID is required");
    }
}