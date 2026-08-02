namespace Application.Features.Causes.CreateCause;

public class CreateCauseValidator : AbstractValidator<CreateCauseCommand>
{
    public CreateCauseValidator()
    {
        RuleFor(x => x.DiseaseId)
            .NotEmpty()
            .WithMessage("Disease ID is required");
        RuleFor(x => x.PathogenId)
            .NotEmpty()
            .WithMessage("Pathogen ID is required");
        RuleFor(x => x.Severity)
            .IsInEnum()
            .WithMessage("Invalid value for severity");
        RuleFor(x => x.TreatmentSite)
            .IsInEnum()
            .WithMessage("Invalid value for treatment site");
    }
}