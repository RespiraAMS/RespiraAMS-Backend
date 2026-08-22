namespace Application.Features.Treatments.CreateTreatment;

public class InfectionProbabilityValidator : AbstractValidator<InfectionProbabilityRecord>
{
    public InfectionProbabilityValidator()
    {
        RuleFor(x => x.Pathogen)
            .NotEmpty()
            .WithMessage("Pathogen is required");
        RuleFor(x => x.Probability)
            .InclusiveBetween(0, 1)
            .WithMessage("Probability must be between 0 and 1");
    }
}

public class MedicalRecordValidator : AbstractValidator<MedicineRecord>
{
    public MedicalRecordValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Medicine name is required");
        RuleFor(x => x.Classification)
            .NotEmpty()
            .WithMessage("Classification is required");
        RuleFor(x => x.RouteOfAdministration)
            .NotEmpty()
            .WithMessage("Route of administration is required");
        RuleFor(x => x.Dose)
            .NotEmpty()
            .WithMessage("Dose is required");
    }
}

public class CreateTreatmentValidator : AbstractValidator<CreateTreatmentCommand>
{
    public CreateTreatmentValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty()
            .WithMessage("Patient ID is required");
        RuleFor(x => x.DoctorId)
            .NotEmpty()
            .WithMessage("Doctor ID is required");
        RuleFor(x => x.MedicineRecords)
            .NotEmpty()
            .WithMessage("Medicine records are required");
        RuleForEach(x => x.MedicineRecords)
            .SetValidator(new MedicalRecordValidator());
        RuleFor(x => x.TreatmentType)
            .NotEmpty()
            .WithMessage("Treatment type is required");
        RuleFor(x => x.Severity)
            .NotEmpty()
            .WithMessage("Severity is required");
        RuleFor(x => x.TreatmentSite)
            .NotEmpty()
            .WithMessage("Treatment site is required");
        RuleForEach(x => x.InfectionProbabilityRecords)
            .SetValidator(new InfectionProbabilityValidator())
            .When(x => x.InfectionProbabilityRecords.Count > 0);
        RuleFor(x => x.Pathogen)
            .NotEmpty()
            .WithMessage("Pathogen is required");
    }
}
