namespace Application.Features.EmpiricTreatmentProtocols.UpdateEmpiricTreatmentProtocol;

public class UpdateEmpiricTreatmentProtocolValidator : AbstractValidator<UpdateEmpiricTreatmentProtocolCommand>
{
    public UpdateEmpiricTreatmentProtocolValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Treatment protocol ID is required");
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Treatment protocol name is required");
        RuleFor(x => x.Issuer)
            .NotEmpty()
            .WithMessage("Treatment protocol issuer is required");
        RuleFor(x => x.IssueDate)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Treatment protocol issue date must not be in future");
        RuleFor(x => x.Version)
            .GreaterThan(0)
            .WithMessage("Treatment protocol version must be greater than 0");
        RuleFor(x => x.Severity)
            .IsInEnum()
            .WithMessage("Invalid value for treatment protocol severity");
        RuleFor(x => x.TreatmentSite)
            .IsInEnum()
            .WithMessage("Invalid value for treatment protocol treatment site");
        RuleFor(x => x.SpecialInfectionId)
            .Must(x => x is null || x != Guid.Empty)
            .WithMessage("Treatment protocol special infection id must not be empty (zero) UUID");
        RuleForEach(x => x.OtherCriteriaIds)
            .NotEmpty()
            .When(x => x.OtherCriteriaIds.Count > 0)
            .WithMessage("Other criteria ID must be a valid UUID");
        RuleFor(x => x.MedicineIds)
            .NotEmpty()
            .WithMessage("Medicine IDs must not be empty");
        RuleForEach(x => x.MedicineIds)
            .NotEmpty()
            .WithMessage("Medicine ID must be a valid UUID");
    }
}