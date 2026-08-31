using Domain.Enums;

namespace Application.Features.Treatments.GetTreatmentById;

public class GetTreatmentByIdQuery(Guid id, Guid patientId) : IQuery
{
    public Guid Id { get; set; } = id;
    public Guid PatientId { get; set; } = patientId;
}

public class DoctorInfo
{
    public required Guid Id { get; set; }
    public required string DisplayName { get; set; }
    public string? Avatar { get; set; }
}

public class PatientInfo
{
    /// <summary>
    /// Patient's ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Patient's full name
    /// </summary>
    public required string FullName { get; set; }

    public required int Age { get; set; }
    public required bool IsMale { get; set; }

    /// <summary>
    /// Patient's medical record code
    /// </summary>
    public required string MedicalRecordCode { get; set; }

    /// <summary>
    /// Patient's status
    /// </summary>
    public required PatientStatus Status { get; set; }
}

public class TreatmentInfo
{
    public required Guid Id { get; set; }
    public required DoctorInfo Doctor { get; set; }
    public required PatientInfo Patient { get; set; }
    public required TreatmentType Type { get; set; }
    public required DiagnosisRecord Diagnosis { get; set; }
}
