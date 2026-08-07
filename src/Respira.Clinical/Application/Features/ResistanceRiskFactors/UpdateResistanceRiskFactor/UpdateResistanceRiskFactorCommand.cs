using Application.Features.Shared.ManageCriterion;

namespace Application.Features.ResistanceRiskFactors.UpdateResistanceRiskFactor;

public class UpdateResistanceRiskFactorCommand : ICommand
{
    /// <summary>
    /// Resistance risk factor ID
    /// </summary>
    public required Guid Id { get; set; }

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
}