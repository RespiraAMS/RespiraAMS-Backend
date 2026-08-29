namespace Domain.Models;

public record DiagnosisRecord
{
    public required decimal Crcl { get; init; }
    public required List<MedicineRecord> SystemRecommendedMedicines { get; init; }
    public required List<MedicineRecord> DoctorChosenMedicines { get; init; }
    public string? ReasonForDifferentChoice { get; init; }
}

public record EmpiricalDiagnosisRecord : DiagnosisRecord
{
    public required string Severity { get; init; }
    public required string TreatmentSite { get; init; }
    public required List<InfectionProbabilityRecord> InfectionProbabilityRecords { get; init; }
}

public record TargetedDiagnosisRecord : DiagnosisRecord
{
    public required PathogenRecord Pathogen { get; init; }
}
