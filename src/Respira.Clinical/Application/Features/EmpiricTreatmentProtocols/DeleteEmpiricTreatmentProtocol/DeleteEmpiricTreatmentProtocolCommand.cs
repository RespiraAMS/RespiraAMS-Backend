namespace Application.Features.EmpiricTreatmentProtocols.DeleteEmpiricTreatmentProtocol;

public class DeleteEmpiricTreatmentProtocolCommand : ICommand
{
    /// <summary>
    /// Empiric treatment protocol ID
    /// </summary>
    public required Guid Id { get; set; }
}