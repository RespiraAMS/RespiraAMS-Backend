using Domain.Enums;
using Severity = Domain.Enums.Severity;

namespace Application.Features.EmpiricTreatmentProtocols.UpdateEmpiricTreatmentProtocol;

public class UpdateEmpiricTreatmentProtocolCommand : ICommand
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Issuer { get; set; }
    public required DateOnly IssueDate { get; set; }
    public required int Version { get; set; }
    public required Severity Severity { get; set; }
    public required TreatmentSite TreatmentSite { get; set; }
    public required Guid? SpecialInfectionId { get; set; }
    public required List<Guid> OtherCriteriaIds { get; set; }
    public required List<Guid> MedicineIds { get; set; }
}