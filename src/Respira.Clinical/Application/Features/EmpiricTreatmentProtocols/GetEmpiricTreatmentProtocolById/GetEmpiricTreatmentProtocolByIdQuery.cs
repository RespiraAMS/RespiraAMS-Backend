using Application.Features.Shared.ManageCriterion;
using Domain.Enums;
using Severity = Domain.Enums.Severity;

namespace Application.Features.EmpiricTreatmentProtocols.GetEmpiricTreatmentProtocolById;

public class GetEmpiricTreatmentProtocolByIdQuery : IQuery
{
    public required Guid Id { get; set; }
}

public class PathogenResult
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}

public class AntibioticResult
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}

public class EmpiricTreatmentProtocolResult
{
    public required Guid Id { get; set; }
    public required DateTimeOffset UpdatedAt { get; set; }
    public required string Name { get; set; }
    public required string Issuer { get; set; }
    public required DateOnly IssueDate { get; set; }
    public required int Version { get; set; }
    public required Severity Severity { get; set; }
    public required TreatmentSite TreatmentSite { get; set; }
    public required PathogenResult? SpecialInfection { get; set; }
    public required List<CriterionItem> OtherCriteria { get; set; }
    public required List<AntibioticResult> Medicines { get; set; }
}