using Application.Features.EmpiricTreatmentProtocols.AddNewCriteria;
using Application.Features.EmpiricTreatmentProtocols.CreateEmpiricTreatmentProtocol;
using Application.Features.EmpiricTreatmentProtocols.UpdateEmpiricTreatmentProtocol;
using Application.Features.Shared.ManageCriterion;
using Domain.Enums;

namespace Respira.Clinical.API.Dtos;

public record CreateEmpiricTreatmentProtocolRequestDto
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
    /// <example>1980-07-21</example>
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
    /// Special infection (which is Pathogen) ID
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

public record UpdateEmpiricTreatmentProtocolRequestDto
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
    /// <example>1980-07-21</example>
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
    /// Special infection (which is Pathogen) ID
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

public record AddNewCriteriaRequestDto
{
    /// <summary>
    /// List of new criteria to be added
    /// </summary>
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