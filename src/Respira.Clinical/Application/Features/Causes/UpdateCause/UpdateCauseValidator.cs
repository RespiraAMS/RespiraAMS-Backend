namespace Application.Features.Causes.UpdateCause;

public class UpdateCauseValidator : AbstractValidator<UpdateCauseCommand>
{
    public UpdateCauseValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("ID is required");
        RuleFor(x => x.Severity)
            .IsInEnum()
            .WithMessage("Invalid value for severity");
        RuleFor(x => x.TreatmentSite)
            .IsInEnum()
            .WithMessage("Invalid value for treatment site");
    }
}