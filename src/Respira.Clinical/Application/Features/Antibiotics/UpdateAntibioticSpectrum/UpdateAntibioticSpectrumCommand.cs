namespace Application.Features.Antibiotics.UpdateAntibioticSpectrum;

public class UpdateAntibioticSpectrumCommand : ICommand
{
    public required Guid Id { get; set; }
    public required List<Guid> PathogenIds { get; set; }
}