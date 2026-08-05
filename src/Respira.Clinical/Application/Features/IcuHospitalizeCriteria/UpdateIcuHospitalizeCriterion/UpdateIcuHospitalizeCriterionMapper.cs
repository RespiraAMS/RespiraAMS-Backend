using Application.Features.Shared.ManageCriterion;

namespace Application.Features.IcuHospitalizeCriteria.UpdateIcuHospitalizeCriterion;

public class UpdateIcuHospitalizeCriterionMapper(IUpdateMapper<Criterion, UpdateCriterionCommand> mapper)
    : IUpdateMapper<IcuHospitalizeCriterion, UpdateIcuHospitalizeCriterionCommand>
{
    public void MapModel(IcuHospitalizeCriterion model, UpdateIcuHospitalizeCriterionCommand command)
    {
        mapper.MapModel(model.Criterion, command.Criterion);
        model.Score = command.Score;
        model.UpdatedAt = DateTimeOffset.UtcNow;
    }
}