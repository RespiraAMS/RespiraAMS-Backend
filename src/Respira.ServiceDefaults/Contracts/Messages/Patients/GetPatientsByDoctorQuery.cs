namespace Respira.ServiceDefaults.Contracts.Messages.Patients;

public sealed record GetPatientsByDoctorQuery
{
    public Guid? DoctorId { get; init; }
    public DateTimeOffset Start { get; init; }
    public DateTimeOffset End { get; init; }
}

public sealed record PatientsByDoctorResult
{
    public IEnumerable<PatientAnalysisItem> Patients { get; init; } = [];
}

public sealed record PatientAnalysisItem
{
    public required Guid Id { get; init; }
    public required string FullName { get; init; }
    public required string PhoneNumber { get; init; }
    public required DateOnly DateOfBirth { get; init; }
    public required string Gender { get; init; }
    public required decimal Height { get; init; }
    public required decimal Weight { get; init; }
    public required string MedicalRecordCode { get; init; }
    public required string HealthInsuranceCard { get; init; }
    public required string Address { get; init; }
    public required string City { get; init; }
    public required DateTimeOffset Admission { get; init; }
    public DateTimeOffset? Discharge { get; init; }
    public required string TreatmentOutcome { get; init; }
    public IEnumerable<NextOfKinAnalysisItem> NextOfKins { get; init; } = [];
    public IEnumerable<TreatmentPlanAnalysisItem> TreatmentPlans { get; init; } = [];
}

public sealed record NextOfKinAnalysisItem(
    Guid Id,
    string FullName,
    string PhoneNumber,
    string Relationship
);

public sealed record TreatmentPlanAnalysisItem(
    Guid Id,
    Guid DoctorId,
    string AdherenceState,
    string TreatmentPlanStatus,
    DateTimeOffset UpdatedAt,
    string? ReasonForMedicineChoice,
    IEnumerable<AntibioticAnalysisItem> Medicines
);

public sealed record AntibioticAnalysisItem(Guid Id, string Name, string Classification);

public static class PatientAnalysisConstants
{
    public static class AdherenceStates
    {
        public const string Adhere = "Adhere";
        public const string NotAdhere = "NotAdhere";
        public const string NotAdhereValid = "NotAdhereValid";
        public const string NotAdhereInvalid = "NotAdhereInvalid";
    }

    public static class TreatmentOutcomes
    {
        public const string InTreatment = "InTreatment";
        public const string Recovered = "Recovered";
        public const string Death = "Death";
        public const string OutOfScope = "OutOfScope";
    }
}
