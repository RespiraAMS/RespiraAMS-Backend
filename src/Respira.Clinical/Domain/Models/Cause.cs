using Domain.Enums;

namespace Domain.Models;

/// <summary>
/// Disease cause. Basically, this class is the joint table between Disease and Pathogen, with extra properties
/// </summary>
public class Cause
{
    /// <summary>
    /// Disease ID
    /// </summary>
    public required Guid DiseaseId { get; set; }

    /// <summary>
    /// Disease
    /// </summary>
    public Disease Disease { get; set; } = null!;

    /// <summary>
    /// Pathogen ID
    /// </summary>
    public required Guid PathogenId { get; set; }

    /// <summary>
    /// Pathogen
    /// </summary>
    public Pathogen Pathogen { get; set; } = null!;

    /// <summary>
    /// Cause severity
    /// </summary>
    public required Severity Severity { get; set; }

    /// <summary>
    /// Cause treatment site
    /// </summary>
    public required TreatmentSite TreatmentSite { get; set; }
}