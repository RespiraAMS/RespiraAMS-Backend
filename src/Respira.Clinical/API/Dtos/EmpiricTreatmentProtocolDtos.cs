using Application.Features.EmpiricTreatmentProtocols.AddNewCriteria;
using Application.Features.EmpiricTreatmentProtocols.CreateEmpiricTreatmentProtocol;
using Application.Features.EmpiricTreatmentProtocols.UpdateEmpiricTreatmentProtocol;
using Application.Features.Shared.ManageCriterion;
using Domain.Enums;

namespace Respira.Clinical.API.Dtos;

public class CreateEmpiricTreatmentProtocolRequestDto
{
    public required string Name { get; set; }
    public required string Issuer { get; set; }
    public required DateOnly IssueDate { get; set; }
    public required int Version { get; set; }
    public required Severity Severity { get; set; }
    public required TreatmentSite TreatmentSite { get; set; }
    public required Guid? SpecialInfectionId { get; set; }
    public required List<Guid> OtherCriteriaIds { get; set; }
    public required List<Guid> MedicineIds { get; set; }

    public CreateEmpiricTreatmentProtocolCommand ToCommand(Guid diseaseId)
    {
        return new CreateEmpiricTreatmentProtocolCommand
        {
            DiseaseId = diseaseId,
            Name = Name,
            Issuer = Issuer,
            IssueDate = IssueDate,
            Version = Version,
            Severity = Severity,
            TreatmentSite = TreatmentSite,
            SpecialInfectionId = SpecialInfectionId,
            OtherCriteriaIds = OtherCriteriaIds,
            MedicineIds = MedicineIds
        };
    }
}

public class UpdateEmpiricTreatmentProtocolRequestDto
{
    public required string Name { get; set; }
    public required string Issuer { get; set; }
    public required DateOnly IssueDate { get; set; }
    public required int Version { get; set; }
    public required Severity Severity { get; set; }
    public required TreatmentSite TreatmentSite { get; set; }
    public required Guid? SpecialInfectionId { get; set; }
    public required List<Guid> OtherCriteriaIds { get; set; }
    public required List<Guid> MedicineIds { get; set; }

    public UpdateEmpiricTreatmentProtocolCommand ToCommand(Guid id)
    {
        return new UpdateEmpiricTreatmentProtocolCommand
        {
            Id = id,
            Name = Name,
            Issuer = Issuer,
            IssueDate = IssueDate,
            Version = Version,
            Severity = Severity,
            TreatmentSite = TreatmentSite,
            SpecialInfectionId = SpecialInfectionId,
            OtherCriteriaIds = OtherCriteriaIds,
            MedicineIds = MedicineIds
        };
    }
}

public class AddNewCriteriaRequestDto
{
    public List<CreateCriterionCommand> Criteria { get; set; } = [];

    public AddNewCriteriaCommand ToCommand(Guid id)
    {
        return new AddNewCriteriaCommand
        {
            Id = id,
            Criteria = Criteria
        };
    }
}