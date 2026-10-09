using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Criteria.GetReferencedEntities
{
    // This feature is used to list all the entities that used this criterion,
    // so that the client know which entities will be affected by the criterion
    // changed
    public class GetReferencedEntitiesHandler(IDbContext context, ILogger<GetReferencedEntitiesHandler> logger)
        : IQueryHandler<GetReferencedEntitiesQuery, Result<ReferencedEntitiesResult>>
    {
        public async Task<Result<ReferencedEntitiesResult>> HandleAsync(GetReferencedEntitiesQuery query, CancellationToken cancellationToken = default)
        {
            // Check if criterion ID exists
            if (await context.Criteria.FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken) is null)
            {
                logger.LogDebug("Criterion not found: {Id}", query.Id);
                return Result<ReferencedEntitiesResult>.Failure(new Error(ApplicationStatus.BadRequest, "Criterion not found"));
            }

            // Get the list of all referenced entities
            var riskFactors = await context.RiskFactors
                .Where(x => x.CriterionId == query.Id)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            var metricsRules = await context.MetricsRules
                .Where(x => x.CriterionId == query.Id)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            var treatments = await context.Treatments
                .Where(x => x.Criteria.Select(x => x.Id).Contains(query.Id))
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            return Result<ReferencedEntitiesResult>.Success(ApplicationStatus.Success, new ReferencedEntitiesResult
            {
                HasReferencedEntities = riskFactors.Count > 0 || metricsRules.Count > 0 || treatments.Count > 0,
                RiskFactorReference = riskFactors,
                MetricsRuleReference = metricsRules,
                TreatmentReference = treatments,
            });
        }
    }
}
