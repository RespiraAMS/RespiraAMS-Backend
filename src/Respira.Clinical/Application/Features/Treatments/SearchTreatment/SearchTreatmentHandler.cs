using Respira.Clinical.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Respira.Clinical.Application.Features.Treatments.SearchTreatment
{
    public class SearchTreatmentHandler(IDbContext context, ILogger<SearchTreatmentHandler> logger)
        : IQueryHandler<SearchTreatmentQuery, Result<SearchTreatmentResult>>
    {
        public async Task<Result<SearchTreatmentResult>> HandleAsync(SearchTreatmentQuery query, CancellationToken cancellationToken = default)
        {
            // Check if pathogen and criteria are valid
            var pathogenDbCount = await context.Pathogens.CountAsync(p => query.PathogenIds.Contains(p.Id), cancellationToken);
            if (pathogenDbCount != query.PathogenIds.Count)
            {
                logger.LogDebug("Not all pathogen IDs given exist in database: {detail}", new
                {
                    TotalIdsGiven = query.PathogenIds.Count,
                    TotalFoundInDb = pathogenDbCount,
                });
                return Result<SearchTreatmentResult>.Failure(new Error(ApplicationStatus.BadRequest, "Not all pathogen IDs given exist"));
            }

            var criteriaDbCount = await context.Criteria.CountAsync(c => query.CriteriaIds.Contains(c.Id), cancellationToken);
            if (criteriaDbCount != query.CriteriaIds.Count)
            {
                logger.LogDebug("Not all criteria IDs given exist in database: {detail}", new
                {
                    TotalIdsGiven = query.CriteriaIds.Count,
                    TotalFoundInDb = criteriaDbCount,
                });
                return Result<SearchTreatmentResult>.Failure(new Error(ApplicationStatus.BadRequest, "Not all criteria IDs given exist"));
            }

            // Empty PathogenIds means no pathogen filter; otherwise the treatment must cover
            // all selected pathogens (treatment.Pathogens ⊆ query.PathogenIds).
            var candidates = await context.Treatments
                .AsNoTracking()
                .Where(x =>
                    x.Severity == query.Severity &&
                    x.TreatmentSite == query.TreatmentSite &&
                    x.Criteria.Select(c => c.Id).All(c => query.CriteriaIds.Contains(c)))
                .Select(x => new
                {
                    x.Id,
                    PathogenIds = x.Pathogens.Select(p => p.Id).ToList(),
                    CriteriaIds = x.Criteria.Select(c => c.Id).ToList(),
                    Solutions = x.Medicines
                        .Select(mc => mc.Antibiotics
                            .Select(a => new MedicineResult(a.Id, a.Name, a.AntibioticGroup.Name))
                            .ToList())
                        .ToList(),
                })
                .ToListAsync(cancellationToken);

            if (query.PathogenIds.Count > 0)
            {
                candidates = candidates
                    .Where(t => t.PathogenIds.All(p => query.PathogenIds.Contains(p)))
                    .ToList();
            }

            var matchedCount = candidates.Count;

            // Dominance: drop T when another matched U covers a strict superset of T's
            // pathogens and U's criteria are a subset of T's criteria (U is at least as
            // applicable but more general on pathogens / less restrictive on criteria).
            var survivors = candidates
                .Where(t => !candidates.Any(u =>
                    u.Id != t.Id &&
                    u.PathogenIds.Count > t.PathogenIds.Count &&
                    u.PathogenIds.All(p => t.PathogenIds.Contains(p)) &&
                    t.CriteriaIds.All(c => u.CriteriaIds.Contains(c))))
                .ToList();

            var dominanceRemovedCount = matchedCount - survivors.Count;

            // Union of solutions across surviving treatments, deduped by composition
            // structure (the set of antibiotics), ordered for deterministic output.
            var solutions = survivors
                .SelectMany(t => t.Solutions)
                .Where(meds => meds.Count > 0)
                .DistinctBy(meds => string.Join(",", meds.Select(m => m.Id).Order()))
                .OrderBy(meds => string.Join(",", meds.Select(m => m.Id).Order()), StringComparer.Ordinal)
                .Select(meds => new TreatmentMedicineResult(meds))
                .ToList();

            logger.LogInformation(
                "Treatment search: severity={Severity}, site={TreatmentSite}, pathogensGiven={PathogenCount}, criteriaGiven={CriteriaCount}, matched={Matched}, dominanceRemoved={DominanceRemoved}, distinctSolutions={DistinctSolutions}",
                query.Severity,
                query.TreatmentSite,
                query.PathogenIds.Count,
                query.CriteriaIds.Count,
                matchedCount,
                dominanceRemovedCount,
                solutions.Count);

            return Result<SearchTreatmentResult>.Success(ApplicationStatus.Success, new SearchTreatmentResult(solutions));
        }
    }
}
