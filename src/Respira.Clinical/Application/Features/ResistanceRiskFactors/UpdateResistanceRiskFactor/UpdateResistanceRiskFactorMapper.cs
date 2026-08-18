using Application.Features.Shared.ManageCriterion;

namespace Application.Features.ResistanceRiskFactors.UpdateResistanceRiskFactor;

public class UpdateResistanceRiskFactorMapper(IUpdateMapper<Criterion, UpdateCriterionCommand> mapper)
    : IUpdateMapper<ResistanceRiskFactor, UpdateResistanceRiskFactorCommand>
{
    public void MapModel(ResistanceRiskFactor model, UpdateResistanceRiskFactorCommand command)
    {
        model.Name = command.Name;
        model.PathogenId = command.PathogenId;
        mapper.MapModel(model.Criterion, command.Criterion);
        model.UpdatedAt = DateTimeOffset.UtcNow;
    }
}
