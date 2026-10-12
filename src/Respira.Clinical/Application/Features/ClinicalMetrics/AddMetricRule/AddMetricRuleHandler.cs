using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.AddMetricRule
{
    public class AddMetricRuleHandler(
        IDbContext context,
        ICreateMapper<AddMetricRuleCommand, MetricRule> mapper,
        ILogger<AddMetricRuleHandler> logger)
        : ICommandHandler<AddMetricRuleCommand, Result<AddMetricRuleResult>>
    {
        public async Task<Result<AddMetricRuleResult>> HandleAsync(AddMetricRuleCommand command, CancellationToken cancellationToken = default)
        {
            // Check if the clinical metric exists
            var metric = await context.ClinicalMetrics
                .Include(x => x.Rules)
                .ThenInclude(x => x.Criterion)
                .FirstOrDefaultAsync(x => x.Id == command.ClinicalMetricId, cancellationToken);
            if (metric is null)
            {
                logger.LogDebug("Clinical metric does not exist: {Id}", command.ClinicalMetricId);
                return Result<AddMetricRuleResult>.Failure(new Error(ApplicationStatus.BadRequest, "Clinical metric does not exist"));
            }

            // Check if the criterion exists
            if (await context.Criteria.FirstOrDefaultAsync(x => x.Id == command.CriterionId, cancellationToken) is null)
            {
                logger.LogDebug("Criterion does not exist: {Id}", command.CriterionId);
                return Result<AddMetricRuleResult>.Failure(new Error(ApplicationStatus.BadRequest, "Criterion does not exist"));
            }

            // Check if the pair (clinical metric, criterion) is unique
            if (metric.Rules.Any(x => x.CriterionId == command.CriterionId))
            {
                logger.LogDebug("There already exists a rule with this criterion {Id} in this metric", command.CriterionId);
                return Result<AddMetricRuleResult>.Failure(new Error(ApplicationStatus.BadRequest, "There already exists a rule with this criterion in this metric"));
            }

            // Business rule: all rules belong to a metric should be of the same type
            // If no rule exists, then no need for check, the first rule added will decide
            // the type of the metric
            if (metric.Rules.Count > 0)
            {
                if (metric.IsScoringMetric && command.ScoreFunction is null)
                {
                    logger.LogDebug("Cannot add major/minor rule to a scoring metric");
                    return Result<AddMetricRuleResult>.Failure(new Error(ApplicationStatus.BusinessRuleViolation, "Cannot add major/minor rule to a scoring metric"));
                }

                if (metric.IsMajorMinorMetric && command.IsMajor is null)
                {
                    logger.LogDebug("Cannot add scoring rule to a major/minor metric");
                    return Result<AddMetricRuleResult>.Failure(new Error(ApplicationStatus.BusinessRuleViolation, "Cannot add scoring rule to a major/minor metric"));
                }
            }

            // Map the command to model
            var variables = await context.ClinicalVariables.ToListAsync(cancellationToken);
            var mapResult = mapper.ToModel(command, variables);
            if (mapResult.IsFailure())
            {
                logger.LogDebug("Failed to map command to model: {error}", mapResult.Error!);
                return Result<AddMetricRuleResult>.Failure(mapResult.Error!);
            }
            var rule = mapResult.Data!;

            // Save changes to database
            await context.MetricRules.AddAsync(rule, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return Result<AddMetricRuleResult>.Success(ApplicationStatus.Created, new AddMetricRuleResult(rule.Id));
        }
    }
}
