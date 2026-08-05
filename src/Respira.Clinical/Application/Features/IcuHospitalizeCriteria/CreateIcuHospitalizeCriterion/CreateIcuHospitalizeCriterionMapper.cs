using Application.Features.Shared.ManageCriterion;

namespace Application.Features.IcuHospitalizeCriteria.CreateIcuHospitalizeCriterion;

public class CreateIcuHospitalizeCriterionMapper(ICreateMapper<Criterion, CreateCriterionCommand> mapper)
    : ICreateMapper<IcuHospitalizeCriterion, CreateIcuHospitalizeCriterionCommand>
{
    public IcuHospitalizeCriterion ToModel(CreateIcuHospitalizeCriterionCommand command)
    {
        var criterion = mapper.ToModel(command.Criterion);

        return new IcuHospitalizeCriterion
        {
            DiseaseId = command.DiseaseId,
            CriterionId = criterion.Id,
            Criterion = criterion,
            Score = command.Score,
        };
    }
}