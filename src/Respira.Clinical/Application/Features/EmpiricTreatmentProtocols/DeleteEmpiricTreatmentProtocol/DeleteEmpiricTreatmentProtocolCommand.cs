namespace Application.Features.EmpiricTreatmentProtocols.DeleteEmpiricTreatmentProtocol;

public class DeleteEmpiricTreatmentProtocolCommand : ICommand
{
    public required Guid Id { get; set; }
}