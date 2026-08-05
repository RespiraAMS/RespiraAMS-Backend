namespace Domain.Models;

/*
 * ICU hospitalize criteria rules:
 * 1. ICU hospitalize criteria differ by disease
 * 2. Each criterion has a score, if the total score surpass a threshold, a patient is recommended to take ICU
 * healthcare (threshold varies depend on disease)
 */

/// <summary>
/// ICU hospitalizing criterion.
/// </summary>
public class IcuHospitalizeCriterion : Base
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
    /// ICU hospitalize criterion ID
    /// </summary>
    public required Guid CriterionId { get; set; }

    /// <summary>
    /// ICU hospitalize criterion
    /// </summary>
    public Criterion Criterion { get; set; } = null!;

    /// <summary>
    /// Criterion score
    /// </summary>
    public required int Score { get; set; }
}