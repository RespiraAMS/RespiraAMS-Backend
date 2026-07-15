using Domain.Enums;

namespace Domain.Models;

/*
 * Treatment protocol have the following traits:
 * 1. All treatment protocol have designated disease, severity and treatment site and medicines
 * 2. Treatment protocol can be design to deal with a dedicated pathogen
 * 3. Treatment protocol can be design to deal with patient with certain conditions
 */

/// <summary>
/// Treatment protocol.
/// </summary>
public class TreatmentProtocol : Base
{
    /// <summary>
    /// Treatment protocol name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Treatment protocol issuer
    /// </summary>
    /// <example>
    /// WHO, VietNam Ministry of Health
    /// </example>
    public required string Issuer { get; set; }

    /// <summary>
    /// Treatment protocol issue date
    /// </summary>
    public required DateOnly IssueDate { get; set; }

    /// <summary>
    /// Treatment protocol version (1-based index)
    /// </summary>
    public required int Version { get; set; }

    /// <summary>
    /// Treatment protocol disease ID
    /// </summary>
    public required Guid DiseaseId { get; set; }

    /// <summary>
    /// Treatment protocol disease
    /// </summary>
    public Disease Disease { get; set; } = null!;

    /// <summary>
    /// Treatment protocol designated severity
    /// </summary>
    public required Severity Severity { get; set; }

    /// <summary>
    /// Treatment protocol designated treatment site
    /// </summary>
    public required TreatmentSite TreatmentSite { get; set; }

    /// <summary>
    /// Special infection ID
    /// </summary>
    public Guid? SpecialInfectionId { get; set; }

    /// <summary>
    /// Spectial infection pathogen
    /// </summary>
    public Pathogen? SpecialInfection { get; set; }

    /// <summary>
    /// List of the sub-criteria IDs
    /// </summary>
    public List<Guid> OtherCriteriaIds { get; set; } = [];

    /// <summary>
    /// List of the sub-criteria
    /// </summary>
    public List<Criterion> OtherCriteria { get; set; } = [];

    /// <summary>
    /// List of designated medicines (antibiotics) IDs
    /// </summary>
    public List<Guid> MedicineIds { get; set; } = [];

    /// <summary>
    /// List of designated medicines (antibiotics)
    /// </summary>
    public List<Antibiotic> Medicines { get; set; } = [];
}