using Application.Features.ResistanceRiskFactors.CreateResistanceRiskFactor;
using Application.Features.ResistanceRiskFactors.UpdateResistanceRiskFactor;
using Application.Features.Shared.ManageCriterion;

namespace Respira.Clinical.API.Dtos;

public record CreateResistanceRiskFactorRequestDto
{
    /// <summary>
    /// Pathogen ID
    /// </summary>
    public required Guid PathogenId { get; set; }

    /// <summary>
    /// Criterion
    /// </summary>
    public required CreateCriterionCommand Criterion { get; set; }

    /// <summary>
    /// Factor name
    /// </summary>
    public required string Name { get; set; }

    public CreateResistanceRiskFactorCommand ToCommand(Guid diseaseId)
    {
        return new CreateResistanceRiskFactorCommand
        {
            DiseaseId = diseaseId,
            PathogenId = PathogenId,
            Criterion = Criterion,
            Name = Name
        };
    }
}

public record UpdateResistanceRiskFactorRequestDto
{
    /// <summary>
    /// Pathogen ID
    /// </summary>
    public required Guid PathogenId { get; set; }

    /// <summary>
    /// Criterion
    /// </summary>
    public required UpdateCriterionCommand Criterion { get; set; }

    /// <summary>
    /// Factor name
    /// </summary>
    public required string Name { get; set; }

    public UpdateResistanceRiskFactorCommand ToCommand(Guid id)
    {
        return new UpdateResistanceRiskFactorCommand
        {
            Id = id,
            PathogenId = PathogenId,
            Criterion = Criterion,
            Name = Name
        };
    }
}