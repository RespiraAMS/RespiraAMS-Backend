namespace Application.Features.Treatments.CreateTreatment;

public class MedicalRecordValidator : AbstractValidator<MedicineRecord>
{
    public MedicalRecordValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Medicine ID is required");
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

public class PathogenValidator : AbstractValidator<Domain.Models.PathogenRecord>
{
    public PathogenValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Pathogen ID is required");
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Pathogen name is required");
    }
}

public class InfectionProbabilityValidator : AbstractValidator<InfectionProbabilityRecord>
{
    public InfectionProbabilityValidator()
    {
        RuleFor(x => x.Pathogen)
            .SetValidator(new PathogenValidator());
        RuleFor(x => x.Probability)
            .InclusiveBetween(0, 1)
            .WithMessage("Probability must be between 0 and 1");
    }
}

public class DiagnosisRecordValidator : AbstractValidator<DiagnosisRecord>
{
    private static bool IsMedicineSame(List<MedicineRecord> a, List<MedicineRecord> b)
    {
        if (a.Count != b.Count)
            return false;

        var sortedA = a.OrderBy(x => x.Id).ToList();
        var sortedB = b.OrderBy(x => x.Id).ToList();

        for (var i = 0; i < sortedA.Count; i++)
        {
            if (sortedA[i] != sortedB[i])
                return false;
        }

        return true;
    }

    public DiagnosisRecordValidator()
    {
        RuleFor(x => x.Crcl)
            .GreaterThan(0)
            .WithMessage("Crcl must be greater than 0");
        RuleFor(x => x.SystemRecommendedMedicines)
            .NotEmpty()
            .WithMessage("System recommended medicines records are required");
        RuleForEach(x => x.SystemRecommendedMedicines)
            .SetValidator(new MedicalRecordValidator());
        RuleFor(x => x.DoctorChosenMedicines)
            .NotEmpty()
            .WithMessage("Doctor chosen medicine records are required");
        RuleForEach(x => x.DoctorChosenMedicines)
            .SetValidator(new MedicalRecordValidator());
        RuleFor(x => x.ReasonForDifferentChoice)
            .Null()
            .WithMessage("Reason for different choice must be null if system recommendation and doctor choice is the same")
            .When(x => IsMedicineSame(x.SystemRecommendedMedicines, x.DoctorChosenMedicines));
        RuleFor(x => x.ReasonForDifferentChoice)
            .NotEmpty()
            .WithMessage("Reason for different choice is required if system recommendation and doctor choice is different")
            .When(x => !IsMedicineSame(x.SystemRecommendedMedicines, x.DoctorChosenMedicines));
    }
}

public class EmpiricalDiagnosisRecordValidator : AbstractValidator<EmpiricalDiagnosisRecord>
{
    public EmpiricalDiagnosisRecordValidator()
    {
        Include(new DiagnosisRecordValidator());
        RuleFor(x => x.Severity)
            .NotEmpty()
            .WithMessage("Severity is required");
        RuleFor(x => x.TreatmentSite)
            .NotEmpty()
            .WithMessage("Treatment site is required");
        RuleForEach(x => x.InfectionProbabilityRecords)
            .SetValidator(new InfectionProbabilityValidator())
            .When(x => x.InfectionProbabilityRecords?.Count > 0);
    }
}

public class TargetedDiagnosisRecordValidator : AbstractValidator<TargetedDiagnosisRecord>
{
    public TargetedDiagnosisRecordValidator()
    {
        Include(new DiagnosisRecordValidator());
        RuleFor(x => x.Pathogen)
            .SetValidator(new PathogenValidator());
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
        RuleFor(x => x.TreatmentType)
            .IsInEnum()
            .WithMessage("Treatment type is required");
        RuleFor(x => x.DiagnosisRecord)
            .SetInheritanceValidator(v =>
            {
                v.Add(new EmpiricalDiagnosisRecordValidator());
                v.Add(new TargetedDiagnosisRecordValidator());
            });
    }
}
