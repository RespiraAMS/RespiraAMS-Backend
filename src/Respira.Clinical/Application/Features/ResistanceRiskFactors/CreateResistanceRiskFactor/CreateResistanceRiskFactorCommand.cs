using Application.Features.Shared.ManageCriterion;

namespace Application.Features.ResistanceRiskFactors.CreateResistanceRiskFactor;

public class CreateResistanceRiskFactorCommand : ICommand
{
    /// <summary>
    /// Disease ID
    /// </summary>
    public required Guid DiseaseId { get; set; }

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
}

public class CreateResistanceRiskFactorResult(Guid id)
{
    /// <summary>
    /// Resistance risk factor ID
    /// </summary>
    public Guid Id { get; set; } = id;
}