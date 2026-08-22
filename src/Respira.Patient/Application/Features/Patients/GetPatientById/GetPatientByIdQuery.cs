using Domain.Enums;

namespace Application.Features.Patients.GetPatientById;

public class GetPatientByIdQuery : IQuery
{
    /// <summary>
    /// Patient's ID
    /// </summary>
    public required Guid Id { get; set; }
}

public class TreatmentResult
{
    /// <summary>
    /// Patient's treatment ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Treatment start time
    /// </summary>
    public required DateTimeOffset Start { get; set; }

    /// <summary>
    /// Treatment type
    /// </summary>
    public required TreatmentType TreatmentType { get; set; }

    /// <summary>
    /// Treatment status. Note that this status is different with patient status
    /// </summary>
    public required PatientTreatmentStatus Status { get; set; }
}

public class PatientResult
{
    /// <summary>
    /// Patient's ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Patient's full name
    /// </summary>
    public required string FullName { get; set; }

    /// <summary>
    /// Patient's date of birth
    /// </summary>
    public required DateOnly DateOfBirth { get; set; }

    /// <summary>
    /// Patient's gender
    /// </summary>
    public required bool IsMale { get; set; }

    /// <summary>
    /// Patient's medical record code
    /// </summary>
    public required string MedicalRecordCode { get; set; }

    /// <summary>
    /// Patient's health insurance card number
    /// </summary>
    public required string HealthInsuranceCardNumber { get; set; }

    /// <summary>
    /// Patient's address
    /// </summary>
    public required string Address { get; set; }

    /// <summary>
    /// Patient's admission time
    /// </summary>
    public required DateTimeOffset Admission { get; set; }

    /// <summary>
    /// Patient's discharge time
    /// </summary>
    public required DateTimeOffset? Discharge { get; set; }

    /// <summary>
    /// Patient's status
    /// </summary>
    public required PatientStatus Status { get; set; } = PatientStatus.InTreatment;

    /// <summary>
    /// Patient's treatment timeline
    /// </summary>
    public required List<TreatmentResult> Treatments { get; set; }
}