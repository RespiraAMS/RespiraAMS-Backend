using Domain.Enums;

namespace Domain.Models;

public class Antibiogram : Base
{
    /// <summary>
    /// Pathogen ID
    /// </summary>
    public required Guid PathogenId { get; set; }

    /// <summary>
    /// Pathogen
    /// </summary>
    public Pathogen Pathogen { get; set; } = null!;

    /// <summary>
    /// MIC level
    /// </summary>
    public required MinimumInhibitoryConcentration MicLevel { get; set; }

    /// <summary>
    /// The list of antibiotic IDs that involve with MIC of this antibiogram
    /// </summary>
    public List<Guid> MicIds { get; set; } = [];

    /// <summary>
    /// The list of antibiotics that involve with MIC of this antibiogram
    /// </summary>
    public List<Antibiotic> Mics { get; set; } = [];
    
    /// <summary>
    /// First priority antibiotic picked IDs
    /// </summary>
    public List<Guid> FirstPriorityMedicineIds { get; set; } = [];
    
    /// <summary>
    /// First priority antibiotic picked
    /// </summary>
    public List<Antibiotic> FirstPriorityMedicines { get; set; } = [];
    
    /// <summary>
    /// Second priority antibiotic picked IDs
    /// </summary>
    public List<Guid> SecondPriorityMedicineIds { get; set; } = [];
    
    /// <summary>
    /// Second priority antibiotic picked
    /// </summary>
    public List<Antibiotic> SecondPriorityMedicines { get; set; } = [];
}