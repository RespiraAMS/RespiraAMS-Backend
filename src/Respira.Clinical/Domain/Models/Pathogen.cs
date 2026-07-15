namespace Domain.Models;

/// <summary>
/// Pathogen
/// </summary>
public class Pathogen : Base
{
    /// <summary>
    /// Pathogen name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Pathogen description
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// List of antibiotic IDs (which corresponse to <see cref="Medicines"/>)
    /// </summary>
    public List<Guid> AntibioticIds { get; set; } = [];

    /// <summary>
    /// List of antibiotics that can affect this pathogen
    /// </summary>
    public List<Antibiotic> Medicines { get; set; } = [];
}