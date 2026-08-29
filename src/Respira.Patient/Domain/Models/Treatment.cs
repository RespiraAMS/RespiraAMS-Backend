using Domain.Enums;
using Respira.ServiceDefaults.Models;

namespace Domain.Models;

/*
 * Treatment class is a history record of patient treatment, which should be:
 * 1. Immutable (except for status that can change). Record, like medicine or patient diagnosis
 * result shouldn't be changed
 * 2. Data should be point in time, which means data should be correct at the time it was created,
 * not the time it was queried. That's why, this service wouldn't reference Clinical service to get
 * data, but instead store duplicate data like name (Doctor ID is an exception, it should still
 * reference the Doctor service to get doctor data)
 */

/// <summary>
/// Patient treatment. This class is a node in the patient treatment timeline. We can simply
/// use the created timestamp to check the node order, so there is no need to design a next/prev
/// pointer like linked list
/// </summary>
public abstract class Treatment : Base
{
    /// <summary>
    /// Doctor ID: ID of the doctor who responsible for this treatment
    /// </summary>
    public required Guid DoctorId { get; set; }

    /// <summary>
    /// Patient ID
    /// </summary>
    public required Guid PatientId { get; set; }

    /// <summary>
    /// Patient
    /// </summary>
    public Patient Patient { get; set; } = null!;

    /// <summary>
    /// Treatment type
    /// </summary>
    public abstract TreatmentType TreatmentType { get; }

    /// <summary>
    /// Patient treatment status. This is the patient status after receive the treatment
    /// (e.g. good response, bad response,...), it's not the same as <see cref="Patient.Status"/>
    /// (any <see cref="PatientTreatmentStatus"/> correspond to <see cref="PatientStatus.InTreatment"/>, to be exact)
    /// </summary>
    public required PatientTreatmentStatus Status { get; set; }

    // Polymorphic, read-only view of the common diagnosis fields
    public abstract DiagnosisRecord DiagnosisRecord { get; }
}

/// <summary>
/// Empirical treatment, which relied on patient symptoms and empiric treatment protocol
/// to treat patient without microbiological test
/// </summary>
public class EmpiricalTreatment : Treatment
{
    public override TreatmentType TreatmentType => TreatmentType.EmpiricalTherapy;

    public required EmpiricalDiagnosisRecord EmpiricalDiagnosisRecord { get; set; }

    public override DiagnosisRecord DiagnosisRecord => EmpiricalDiagnosisRecord;

}

/// <summary>
/// Targeted treatment, which treat patient after microbiological test and identify cause, used
/// antibiogram for this treatment
/// </summary>
public class TargetedTreatment : Treatment
{
    public override TreatmentType TreatmentType => TreatmentType.TargetedTherapy;

    public required TargetedDiagnosisRecord TargetedDiagnosisRecord { get; set; }

    public override DiagnosisRecord DiagnosisRecord => TargetedDiagnosisRecord;
}
