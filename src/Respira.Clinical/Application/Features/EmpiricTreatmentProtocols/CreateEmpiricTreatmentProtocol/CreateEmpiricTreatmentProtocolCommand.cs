using Domain.Enums;
using Severity = Domain.Enums.Severity;

namespace Application.Features.EmpiricTreatmentProtocols.CreateEmpiricTreatmentProtocol;

public class CreateEmpiricTreatmentProtocolCommand : ICommand
{
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
    /// Disease ID
    /// </summary>
    public required Guid DiseaseId { get; set; }

    /// <summary>
    /// Severity assigned to this empiric treatment protocol
    /// </summary>
    public required Severity Severity { get; set; }

    /// <summary>
    /// Treatment site assigned to this empiric treatment protocol
    /// </summary>
    public required TreatmentSite TreatmentSite { get; set; }

    /// <summary>
    /// Special infection (<see cref="Pathogen"/>) ID
    /// </summary>
    public required Guid? SpecialInfectionId { get; set; }

    /// <summary>
    /// List of secondary criteria IDs
    /// </summary>
    public required List<Guid> OtherCriteriaIds { get; set; }

    /// <summary>
    /// List of antibiotic IDs
    /// </summary>
    public required List<Guid> MedicineIds { get; set; }
}

public class CreateEmpiricTreatmentProtocolResult(Guid id)
{
    /// <summary>
    /// Empiric treatment protocol ID
    /// </summary>
    public Guid Id { get; set; } = id;
}