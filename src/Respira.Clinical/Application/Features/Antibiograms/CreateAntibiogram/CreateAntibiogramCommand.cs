using Domain.Enums;

namespace Application.Features.Antibiograms.CreateAntibiogram;

public record CreateAntibiogramCommand : ICommand
{
    /// <summary>
    /// Pathogen ID
    /// </summary>
    public required Guid PathogenId { get; set; }

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

public record CreateAntibiogramResult(Guid Id)
{
    /// <summary>
    /// The created antibiogram ID
    /// </summary>
    public Guid Id { get; set; } = Id;
}
