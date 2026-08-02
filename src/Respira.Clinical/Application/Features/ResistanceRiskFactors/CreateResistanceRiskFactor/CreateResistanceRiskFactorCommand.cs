using Application.Features.Shared.ManageCriterion;

namespace Application.Features.ResistanceRiskFactors.CreateResistanceRiskFactor;

public class CreateResistanceRiskFactorCommand : ICommand
{
    public required Guid DiseaseId { get; set; }
    public required Guid PathogenId { get; set; }
    public required CreateCriterionCommand Criterion { get; set; }
    public required string Name { get; set; }
}

public class CreateResistanceRiskFactorResult(Guid id)
{
    public Guid Id { get; set; } = id;
}