namespace Application.Features.EmpiricTreatmentProtocols.CreateEmpiricTreatmentProtocol;

public class CreateEmpiricTreatmentProtocolMapper
    : ICreateMapper<EmpiricTreatmentProtocol, CreateEmpiricTreatmentProtocolCommand>
{
    public EmpiricTreatmentProtocol ToModel(CreateEmpiricTreatmentProtocolCommand command)
    {
        return new EmpiricTreatmentProtocol
        {
            Name = command.Name,
            Issuer = command.Issuer,
            IssueDate = command.IssueDate,
            Version = command.Version,
            DiseaseId = command.DiseaseId,
            Severity = command.Severity,
            TreatmentSite = command.TreatmentSite,
            SpecialInfectionId = command.SpecialInfectionId
            // The 2 IDs list are ignore by EF Core, it should be added via navigation
        };
    }
}
