using Application.Features.Shared.ManageCriterion;
using Domain.Enums;
using Severity = Domain.Enums.Severity;

namespace Application.Features.Diseases.GetDiseaseById;

public record GetDiseaseByIdQuery(Guid Id) : IQuery
{
    /// <summary>
    /// Disease ID
    /// </summary>
    public Guid Id { get; set; } = Id;
}

public record IcuHospitalizeCriterionResult
{
    /// <summary>
    /// ICU hospitalize criterion ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Criterion
    /// </summary>
    public required CriterionItem Criterion { get; set; }

    /// <summary>
    /// ICU hospitalize criterion score
    /// </summary>
    public required int Score { get; set; }
}

public record CauseResult
{
    /// <summary>
    /// Disease's cause ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Pathogen name
    /// </summary>
    public required string PathogenName { get; set; }

    /// <summary>
    /// Severity caused by this pathogen
    /// </summary>
    public required Severity Severity { get; set; }

    /// <summary>
    /// Treatment site assigned to patient when catching the disease with this pathogen
    /// </summary>
    public required TreatmentSite TreatmentSite { get; set; }
}

public record ResistanceRiskFactorResult
{
    /// <summary>
    /// Resistance risk factor ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Pathogen name
    /// </summary>
    public required string PathogenName { get; set; }

    /// <summary>
    /// Criterion
    /// </summary>
    public required CriterionItem Criterion { get; set; }

    /// <summary>
    /// Factor's name
    /// </summary>
    public required string Name { get; set; }
}

public record EmpiricTreatmentProtocolResult
{
    /// <summary>
    /// Empiric treatment protocol ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Empiric treatment protocol updated timestamp
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Empiric treatment protocol name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Empiric treatment protocol issuer
    /// </summary>
    /// <example>WHO, Vietnam Health Ministry</example>
    public required string Issuer { get; set; }

    /// <summary>
    /// Empiric treatment protocol issue date
    /// </summary>
    public required DateOnly IssueDate { get; set; }

    /// <summary>
    /// Empiric treatment protocol version
    /// </summary>
    public required int Version { get; set; }
}

public record DiseaseResult
{
    /// <summary>
    /// Disease ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Disease name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Disease description
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// ICU score minimum threshold (&gt;= threshold) to consider needing ICU hospitalization
    /// </summary>
    public required int IcuScoreThreshold { get; set; }

    /// <summary>
    /// ICU hospitalize criteria
    /// </summary>
    public required List<IcuHospitalizeCriterionResult> IcuHospitalizeCriteria { get; init; }

    /// <summary>
    /// Resistance risk factors
    /// </summary>
    public required List<ResistanceRiskFactorResult> ResistanceRiskFactors { get; init; }

    /// <summary>
    /// Disease's causes
    /// </summary>
    public required List<CauseResult> Causes { get; init; }

    /// <summary>
    /// Disease's empiric treatment protocol
    /// </summary>
    public required List<EmpiricTreatmentProtocolResult> EmpiricTreatmentProtocols { get; init; }
}
