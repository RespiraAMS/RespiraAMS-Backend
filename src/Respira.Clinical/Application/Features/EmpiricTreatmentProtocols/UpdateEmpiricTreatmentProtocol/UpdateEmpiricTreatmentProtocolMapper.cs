namespace Application.Features.EmpiricTreatmentProtocols.UpdateEmpiricTreatmentProtocol;

public class UpdateEmpiricTreatmentProtocolMapper
    : IUpdateMapper<EmpiricTreatmentProtocol, UpdateEmpiricTreatmentProtocolCommand>
{
    public void MapModel(EmpiricTreatmentProtocol model, UpdateEmpiricTreatmentProtocolCommand command)
    {
        model.Name = command.Name;
        model.Issuer = command.Issuer;
        model.IssueDate = command.IssueDate;
        model.Version = command.Version;
        model.Severity = command.Severity;
        model.TreatmentSite = command.TreatmentSite;
        model.SpecialInfectionId = command.SpecialInfectionId;
        model.UpdatedAt = DateTimeOffset.UtcNow;
        // The 2 IDs list are ignore by EF Core, it should be added via navigation
    }
}