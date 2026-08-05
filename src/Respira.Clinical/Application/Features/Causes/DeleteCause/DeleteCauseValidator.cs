namespace Application.Features.Causes.DeleteCause;

public class DeleteCauseValidator : AbstractValidator<DeleteCauseCommand>
{
    public DeleteCauseValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("ID is required");
    }
}