namespace Application.Features.Antibiotics.UpdateAntibioticSpectrum;

public record UpdateAntibioticSpectrumCommand : ICommand
{
    /// <summary>
    /// Antibiotic ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// List of pathogen IDs
    /// </summary>
    public required List<Guid> PathogenIds { get; set; }
}