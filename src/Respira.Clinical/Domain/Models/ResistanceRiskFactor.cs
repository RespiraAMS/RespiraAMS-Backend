namespace Domain.Models;

/*
 * For resistance risk factors, it's used to calculate the probability of having infected with certain
 * pathogen based on patient condition.
 */

/// <summary>
/// Factor to determine the risk of having infected with a special pathogen that have
/// resistance to antibiotic. Just like ICU hospitalize criteria, these factors are
/// also tied to disease
/// </summary>
public class ResistanceRiskFactor : Base
{
    /// <summary>
    /// Disease ID
    /// </summary>
    public required Guid DiseaseId { get; init; }

    /// <summary>
    /// Disease
    /// </summary>
    public Disease Disease { get; set; } = null!;

    /// <summary>
    /// Factor name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Criterion ID
    /// </summary>
    public required Guid CriterionId { get; set; }

    /// <summary>
    /// Criterion
    /// </summary>
    public Criterion Criterion { get; set; } = null!;

    /// <summary>
    /// Pathogen ID
    /// </summary>
    public required Guid PathogenId { get; set; }

    /// <summary>
    /// Pathogen
    /// </summary>
    public Pathogen Pathogen { get; set; } = null!;
}