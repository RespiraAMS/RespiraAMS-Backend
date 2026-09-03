using Application.Features.Shared.ManageCriterion;
using Domain.Enums;
using Severity = Domain.Enums.Severity;

namespace Application.Features.EmpiricTreatmentProtocols.GetEmpiricTreatmentProtocolById;

public record GetEmpiricTreatmentProtocolByIdQuery(Guid Id) : IQuery
{
    /// <summary>
    /// Empiric treatment protocol ID
    /// </summary>
    public Guid Id { get; set; } = Id;
}

public record PathogenResult
{
    /// <summary>
    /// Pathogen ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Pathogen name
    /// </summary>
    public required string Name { get; set; }
}

public record AntibioticResult
{
    /// <summary>
    /// Antibiotic ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Antibiotic name
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

    /// <summary>
    /// Severity assigned to this empiric treatment protocol
    /// </summary>
    public required Severity Severity { get; set; }

    /// <summary>
    /// Treatment site assigned to this empiric treatment protocol
    /// </summary>
    public required TreatmentSite TreatmentSite { get; set; }

    /// <summary>
    /// Special infection (<see cref="Pathogen"/>)
    /// </summary>
    public required PathogenResult? SpecialInfection { get; set; }

    /// <summary>
    /// List of secondary criteria
    /// </summary>
    public required List<CriterionItem> OtherCriteria { get; set; }

    /// <summary>
    /// List of antibiotics
    /// </summary>
    public required List<AntibioticResult> Medicines { get; set; }
}