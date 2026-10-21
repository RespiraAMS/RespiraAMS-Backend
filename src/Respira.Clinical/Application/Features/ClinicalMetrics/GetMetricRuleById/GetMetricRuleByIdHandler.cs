using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.GetMetricRuleById
{
    public class GetMetricRuleByIdHandler(
        IDbContext context,
        IMapper<Formula, FormulaDto> mapper,
        ILogger<GetMetricRuleByIdHandler> logger)
        : IQueryHandler<GetMetricRuleByIdQuery, Result<MetricRuleResult>>
    {
        public async Task<Result<MetricRuleResult>> HandleAsync(GetMetricRuleByIdQuery query, CancellationToken cancellationToken = default)
        {
            var rule = await context.MetricRules
                .AsNoTracking()
                .Include(x => x.ClinicalMetric)
                .Include(x => x.Criterion)
                .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken);

            if (rule is null)
            {
                return Result<MetricRuleResult>.Failure(new ApplicationError(ApplicationStatus.ResourceNotFound, "Metric rule does not exist"));
            }

            FormulaDto? scoreFunctionFormula = null;
            if (rule is ScoringRule scoring)
            {
                var mapResult = mapper.Map(scoring.ScoreFunction);
                if (mapResult.IsFailure())
                {
                    logger.LogDebug("Failed to map scoring rule score function: {error}", mapResult.Error!);
                    return Result<MetricRuleResult>.Failure(mapResult.Error!);
                }
                scoreFunctionFormula = mapResult.Data;
            }

            return Result<MetricRuleResult>.Success(ApplicationStatus.Success, new MetricRuleResult
            {
                Id = rule.Id,
                ClinicalMetricId = rule.ClinicalMetricId,
                ClinicalMetricName = rule.ClinicalMetric.Name,
                Criterion = new CriterionResult(rule.CriterionId, rule.Criterion.Name, rule.Criterion.Formula.ToString()!),
                ScoreFunction = rule is ScoringRule scoringRule ? scoringRule.ScoreFunction.ToString() : null,
                ScoreFunctionFormula = scoreFunctionFormula,
                IsMajor = rule is MajorMinorRule majorMinorRule ? majorMinorRule.IsMajor : null,
            });
        }
    }
}
