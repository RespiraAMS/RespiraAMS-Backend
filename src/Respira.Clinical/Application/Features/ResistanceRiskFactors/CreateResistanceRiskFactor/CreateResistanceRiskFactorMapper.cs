using Application.Features.Shared.ManageCriterion;

namespace Application.Features.ResistanceRiskFactors.CreateResistanceRiskFactor;

public class CreateResistanceRiskFactorMapper(ICreateMapper<Criterion, CreateCriterionCommand> mapper)
    : ICreateMapper<ResistanceRiskFactor, CreateResistanceRiskFactorCommand>
{
    public ResistanceRiskFactor ToModel(CreateResistanceRiskFactorCommand command)
    {
        var criterion = mapper.ToModel(command.Criterion);

        return new ResistanceRiskFactor
        {
            DiseaseId = command.DiseaseId,
            Name = command.Name,
            CriterionId = criterion.Id,
            Criterion = criterion,
            PathogenId = command.PathogenId
        };
    }
}