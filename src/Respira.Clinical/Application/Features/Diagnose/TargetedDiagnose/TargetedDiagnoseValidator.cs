namespace Application.Features.Diagnose.TargetedDiagnose;

public class TargetedDiagnoseValidator : AbstractValidator<TargetedDiagnoseQuery>
{
    public TargetedDiagnoseValidator()
    {
        RuleFor(x => x.PathogenId)
            .NotEmpty()
            .WithMessage("Pathogen ID is required");
        RuleFor(x => x.Weight)
            .GreaterThan(0)
            .WithMessage("Patient's weight (kg) must be greater than 0");
        RuleFor(x => x.Height)
            .GreaterThan(0)
            .WithMessage("Patient's height (m) must be greater than 0");
        RuleFor(x => x.SerumCreatine)
            .GreaterThan(0)
            .WithMessage("Serum creatine used for patient must be greater than 0");
        RuleFor(x => x.DateOfBirth)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Patient's date of birth must not exceed today");
    }
}
