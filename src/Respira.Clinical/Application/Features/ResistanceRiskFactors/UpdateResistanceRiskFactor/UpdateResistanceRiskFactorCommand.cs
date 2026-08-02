using Application.Features.Shared.ManageCriterion;

namespace Application.Features.ResistanceRiskFactors.UpdateResistanceRiskFactor;

public class UpdateResistanceRiskFactorCommand : ICommand
{
    public required Guid Id { get; set; }
    public required Guid PathogenId { get; set; }
    public required UpdateCriterionCommand Criterion { get; set; }
    public required string Name { get; set; }
}