using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.AddMetricRule
{
    public class AddMetricRuleMapper(IMapper<FormulaDto, Formula> formulaMapper)
        : ICreateMapper<AddMetricRuleCommand, MetricRule>
    {
        public Result<MetricRule> ToModel(AddMetricRuleCommand command)
        {
            throw new NotImplementedException("This method won't be implemented since it required external dependencies to work");
        }

        public Result<MetricRule> ToModel(AddMetricRuleCommand command, object dependencies)
        {
            if (command.ScoreFunction is not null && command.IsMajor is not null)
            {
                throw new ArgumentException("Only one of score function or major/minor can be set");
            }

            if (command.ScoreFunction is null && command.IsMajor is null)
            {
                throw new ArgumentException("One of score function or major/minor must be set");
            }

            // Map score function if provided
            if (command.ScoreFunction is not null)
            {
                var mapResult = formulaMapper.Map(command.ScoreFunction, dependencies as List<ClinicalVariable>);
                if (mapResult.IsFailure())
                {
                    return Result<MetricRule>.Failure(mapResult.Error!);
                }

                return Result<MetricRule>.Success(ApplicationStatus.Success, new ScoringRule
                {
                    ClinicalMetricId = command.ClinicalMetricId,
                    CriterionId = command.CriterionId,
                    ScoreFunction = mapResult.Data!,
                });
            }

            return Result<MetricRule>.Success(ApplicationStatus.Success, new MajorMinorRule
            {
                ClinicalMetricId = command.ClinicalMetricId,
                CriterionId = command.CriterionId,
                IsMajor = command.IsMajor!.Value,
            });
        }
    }
}
