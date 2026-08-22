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
 * 3. For validation, we will trust the caller (client) to send valid data (since diagnosis result
 * is not stored anyway -> no way to check if the data is actually correct, so it's no point to
 * check for validity)
 */

/// <summary>
/// An immutable record of medicine used for treatment
/// </summary>
/// <param name="Name">Antibiotic name</param>
/// <param name="Classification">Antibiotic classification (WHO's AWaRe classification)</param>
/// <param name="RouteOfAdministration">Antibiotic route of administration</param>
/// <param name="Dose">
/// Actual dosage that was diagnosed for this patient, based on patient's GFR
/// </param>
public record MedicineRecord(string Name, string Classification, string RouteOfAdministration, string Dose);

/// <summary>
/// An immutable record of infection probability when doing empiric therapy
/// </summary>
/// <param name="Pathogen">Pathogen name</param>
/// <param name="Probability">Probability (from 0 to 1)</param>
public record InfectionProbabilityRecord(string Pathogen, double Probability);

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
    /// List of medicines used for this treatment. This list must not empty
    /// </summary>
    public required List<MedicineRecord> MedicineRecords { get; set; }

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
}

/// <summary>
/// Empirical treatment, which relied on patient symptoms and empiric treatment protocol
/// to treat patient without microbiological test
/// </summary>
public class EmpiricalTreatment : Treatment
{
    public override TreatmentType TreatmentType => TreatmentType.EmpiricalTherapy;

    /// <summary>
    /// Patient diagnosis result: severity
    /// </summary>
    public required string Severity { get; set; }

    /// <summary>
    /// Patient diagnosis result: treatment site
    /// </summary>
    public required string TreatmentSite { get; set; }

    /// <summary>
    /// Patient diagnosis result: suspected infection probabilities
    /// </summary>
    public List<InfectionProbabilityRecord> InfectionProbabilityRecords { get; set; } = [];
}

/// <summary>
/// Targeted treatment, which treat patient after microbiological test and identify cause, used
/// antibiogram for this treatment
/// </summary>
public class TargetedTreatment : Treatment
{
    public override TreatmentType TreatmentType => TreatmentType.TargetedTherapy;

    /// <summary>
    /// Name of the pathogen that cause patient to got disease
    /// </summary>
    public required string Pathogen { get; set; }
}
