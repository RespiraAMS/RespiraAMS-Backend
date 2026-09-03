using Domain.Enums;

namespace Application.Features.Antibiograms.UpdateAntibiogram;

public record UpdateAntibiogramCommand : ICommand
{
    /// <summary>
    /// Antibiogram ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Minimum inhibitory concentration level
    /// </summary>
    public required MinimumInhibitoryConcentration MicLevel { get; set; }

    /// <summary>
    /// List of antibiotic IDs that corresponding to MIC level
    /// </summary>
    public required List<Guid> MicIds { get; set; }

    /// <summary>
    /// List of antibiotic IDs that should be first prioritize when using for treatment
    /// </summary>
    public required List<Guid> FirstPriorityMedicineIds { get; set; } = [];

    /// <summary>
    /// List of antibiotic IDs that should be secondary prioritize when using for treatment
    /// </summary>
    public required List<Guid> SecondPriorityMedicineIds { get; set; } = [];
}
