using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Treatments.SearchTreatment
{
    public class SearchTreatmentHandler(IDbContext context, ILogger<SearchTreatmentHandler> logger)
        : IQueryHandler<SearchTreatmentQuery, Result<SearchTreatmentResult>>
    {
        public async Task<Result<SearchTreatmentResult>> HandleAsync(
            SearchTreatmentQuery query,
            CancellationToken cancellationToken = default
        )
        {
            // Check if pathogen and criteria are valid
            var pathogenDbCount = await context.Pathogens.CountAsync(
                p => query.PathogenIds.Contains(p.Id),
                cancellationToken
            );
            if (pathogenDbCount != query.PathogenIds.Count)
            {
                logger.LogDebug(
                    "Not all pathogen IDs given exist in database: {detail}",
                    new
                    {
                        TotalIdsGiven = query.PathogenIds.Count,
                        TotalFoundInDb = pathogenDbCount,
                    }
                );
                return Result<SearchTreatmentResult>.Failure(
                    new Error(ApplicationStatus.BadRequest, "Not all pathogen IDs given exist")
                );
            }

            var criteriaDbCount = await context.Criteria.CountAsync(
                c => query.CriteriaIds.Contains(c.Id),
                cancellationToken
            );
            if (criteriaDbCount != query.CriteriaIds.Count)
            {
                logger.LogDebug(
                    "Not all criteria IDs given exist in database: {detail}",
                    new
                    {
                        TotalIdsGiven = query.CriteriaIds.Count,
                        TotalFoundInDb = criteriaDbCount,
                    }
                );
                return Result<SearchTreatmentResult>.Failure(
                    new Error(ApplicationStatus.BadRequest, "Not all criteria IDs given exist")
                );
            }

            // Empty PathogenIds means no pathogen filter; otherwise the treatment must cover
            // all selected pathogens (treatment.Pathogens ⊆ query.PathogenIds).
            var candidates = await context
                .Treatments.AsNoTracking()
                .Where(x =>
                    x.Severity == query.Severity
                    && x.TreatmentSite == query.TreatmentSite
                    && x.Criteria.Select(c => c.Id).All(c => query.CriteriaIds.Contains(c))
                )
                .Select(x => new TreatmentCandidate(
                    x.Id,
                    x.Pathogens.Select(p => p.Id).ToList(),
                    x.Criteria.Select(c => c.Id).ToList(),
                    x.Medicines.Select(mc =>
                            mc.Antibiotics.Select(a => new MedicineResult(
                                    a.Id,
                                    a.Name,
                                    a.AntibioticGroup.Name
                                ))
                                .ToList()
                        )
                        .ToList()
                ))
                .ToListAsync(cancellationToken);

            if (query.PathogenIds.Count > 0)
            {
                candidates =
                [
                    .. candidates.Where(t => t.PathogenIds.All(p => query.PathogenIds.Contains(p))),
                ];
            }

            var matchedCount = candidates.Count;

            // Dominance: hide a narrower treatment when a wider one is applicable
            // (see TreatmentDominance.Filter). e.g. with pathogens [Pseudomonas, S. aureus],
            // the Pseudomonas-only treatment is hidden in favour of the
            // Pseudomonas + S. aureus treatment; if no combo exists, the single stays.
            var survivors = TreatmentDominance.Filter(candidates, query.PathogenIds);

            var dominanceRemovedCount = matchedCount - survivors.Count;

            // Union of solutions across surviving treatments, deduped by composition
            // structure (the set of antibiotics), ordered for deterministic output.
            var solutions = survivors
                .SelectMany(t => t.Solutions)
                .Where(meds => meds.Count > 0)
                .DistinctBy(meds => string.Join(",", meds.Select(m => m.Id).Order()))
                .OrderBy(
                    meds => string.Join(",", meds.Select(m => m.Id).Order()),
                    StringComparer.Ordinal
                )
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
                solutions.Count
            );

            return Result<SearchTreatmentResult>.Success(
                ApplicationStatus.Success,
                new SearchTreatmentResult(solutions)
            );
        }
    }
}
