namespace Domain.Models;

/*
 * Antibiotic can be category by antibiotic group (which category using chemical structure).
 * A group can have multiple subgroup. For example: penicilin is a subgroup of beta-lactam.
 * Here, we won't categorize group further using antibiotic spectrum
 */

/// <summary>
/// Antibiotic group
/// </summary>
public class AntibioticGroup : Base
{
    /// <summary>
    /// Antibiotic group name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Antibiotic group description
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Antibiotic parent group's ID
    /// </summary>
    public required Guid? ParentId { get; set; }

    /// <summary>
    /// Antibiotic parent group
    /// </summary>
    public AntibioticGroup? Parent { get; set; }
}