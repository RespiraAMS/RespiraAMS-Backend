namespace Application.Features.EmpiricTreatmentProtocols.DeleteEmpiricTreatmentProtocol;

public record DeleteEmpiricTreatmentProtocolCommand(Guid Id) : ICommand
{
    /// <summary>
    /// Empiric treatment protocol ID
    /// </summary>
    public Guid Id { get; set; } = Id;
}