using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.RiskFactors.UpdateRiskFactor
{
    public class UpdateRiskFactorMapper : IUpdateMapper<RiskFactor, UpdateRiskFactorCommand>
    {
        public Result MapModel(RiskFactor model, UpdateRiskFactorCommand command)
        {
            model.PathogenId = command.PathogenId;
            model.CriterionId = command.CriterionId;
            model.UpdatedAt = DateTimeOffset.UtcNow;
            return Result.Success(ApplicationStatus.Success);
        }

        public Result MapModel(RiskFactor model, UpdateRiskFactorCommand command, object? dependencies = null)
        {
            return MapModel(model, command);
        }
    }
}
