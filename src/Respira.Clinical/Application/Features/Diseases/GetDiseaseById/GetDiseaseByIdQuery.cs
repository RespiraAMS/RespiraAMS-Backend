using Application.Features.Shared.ManageCriterion;
using Domain.Enums;
using Severity = Domain.Enums.Severity;

namespace Application.Features.Diseases.GetDiseaseById;

public class GetDiseaseByIdQuery : IQuery
{
    public required Guid Id { get; set; }
}

public class IcuHospitalizeCriterionResult
{
    public required Guid Id { get; set; }
    public required CriterionItem Criterion { get; set; }
    public required int Score { get; set; }
}

public class CauseResult
{
    public required Guid Id { get; set; }
    public required string PathogenName { get; set; }
    public required Severity Severity { get; set; }
    public required TreatmentSite TreatmentSite { get; set; }
}

public class ResistanceRiskFactorResult
{
    public required Guid Id { get; set; }
    public required string PathogenName { get; set; }
    public required CriterionItem Criterion { get; set; }
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
}

public class DiseaseResult
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required int IcuScoreThreshold { get; set; }
    public required List<IcuHospitalizeCriterionResult> IcuHospitalizeCriteria { get; init; }
    public required List<ResistanceRiskFactorResult> ResistanceRiskFactors { get; init; }
    public required List<CauseResult> Causes { get; init; }
    public required List<EmpiricTreatmentProtocolResult> EmpiricTreatmentProtocols { get; init; }
}