using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.UpdateMetricRule
{
    public class UpdateMetricRuleMapper(IMapper<FormulaDto, Formula> formulaMapper)
        : IUpdateMapper<MetricRule, UpdateMetricRuleCommand>
    {
        public Result MapModel(MetricRule model, UpdateMetricRuleCommand command)
        {
            throw new NotImplementedException();
        }

        public Result MapModel(MetricRule model, UpdateMetricRuleCommand command, object? dependencies = null)
        {
            if (command.ScoreFunction is not null && command.IsMajor is not null)
            {
                throw new ArgumentException("Only one of score function or major/minor can be set");
            }

            if (command.ScoreFunction is null && command.IsMajor is null)
            {
                throw new ArgumentException("One of score function or major/minor must be set");
            }

            model.CriterionId = command.CriterionId;
            model.UpdatedAt = DateTimeOffset.UtcNow;

            // Map score function if provided
            if (command.ScoreFunction is not null)
            {
                var mapResult = formulaMapper.Map(command.ScoreFunction, dependencies as List<ClinicalVariable>);
                if (mapResult.IsFailure())
                {
                    return Result.Failure(mapResult.Error!);
                }

                ((ScoringRule)model).ScoreFunction = mapResult.Data!;
            }
            else
            {
                ((MajorMinorRule)model).IsMajor = command.IsMajor!.Value;
            }

            return Result.Success(ApplicationStatus.Success);
        }
    }
}
