using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.RiskFactors.CreateRiskFactor
{
    public class CreateRiskFactorMapper : ICreateMapper<CreateRiskFactorCommand, RiskFactor>
    {
        public Result<RiskFactor> ToModel(CreateRiskFactorCommand command)
        {
            return Result<RiskFactor>.Success(ApplicationStatus.Success, new RiskFactor
            {
                PathogenId = command.PathogenId,
                CriterionId = command.CriterionId
            });
        }

        public Result<RiskFactor> ToModel(CreateRiskFactorCommand command, object? dependencies = null)
        {
            return ToModel(command);
        }
    }
}
