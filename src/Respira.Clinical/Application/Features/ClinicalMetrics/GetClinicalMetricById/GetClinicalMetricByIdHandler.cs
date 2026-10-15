using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.GetClinicalMetricById
{
    public class GetClinicalMetricByIdHandler(IDbContext context)
        : IQueryHandler<GetClinicalMetricByIdQuery, Result<ClinicalMetricResult>>
    {
        public async Task<Result<ClinicalMetricResult>> HandleAsync(GetClinicalMetricByIdQuery query, CancellationToken cancellationToken = default)
        {
            var metric = await context.ClinicalMetrics
                .AsNoTracking()
                .Include(x => x.Rules)
                .ThenInclude(r => r.Criterion)
                .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken);
            if (metric is null)
            {
                return Result<ClinicalMetricResult>.Failure(new Error(ApplicationStatus.ResourceNotFound, "Clinical metric not found"));
            }

            return Result<ClinicalMetricResult>.Success(ApplicationStatus.Success, new ClinicalMetricResult
            {
                Id = metric.Id,
                Name = metric.Name,
                Code = metric.Code,
                Description = metric.Description,
                Version = metric.Version,
                Publisher = metric.Publisher,
                IsApplied = metric.IsApplied,
                Rules = metric.Rules.Select(r => new MetricRuleItem
                {
                    Id = r.Id,
                    Criterion = new CriterionItem(r.CriterionId, r.Criterion.Name, r.Criterion.Formula.ToString()!),
                    ScoreFunction = r is ScoringRule scoreRule ? scoreRule.ScoreFunction.ToString() : null,
                    IsMajorRule = r is MajorMinorRule majorRule ? majorRule.IsMajor : null,
                })
            });
        }
    }
}
