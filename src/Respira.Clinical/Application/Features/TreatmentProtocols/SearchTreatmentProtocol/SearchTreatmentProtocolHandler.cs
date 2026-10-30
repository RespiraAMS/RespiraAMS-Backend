using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.TreatmentProtocols.SearchTreatmentProtocol
{
    public class SearchTreatmentProtocolHandler(IDbContext context, ILogger<SearchTreatmentProtocolHandler> logger)
        : IQueryHandler<SearchTreatmentProtocolQuery, Result<SearchTreatmentResult>>
    {
        public async Task<Result<SearchTreatmentResult>> HandleAsync(
            SearchTreatmentProtocolQuery protocolQuery,
            CancellationToken cancellationToken = default
        )
        {
            // Check if pathogen and criteria are valid
            var pathogenDbCount = await context.Pathogens.CountAsync(
                p => protocolQuery.PathogenIds.Contains(p.Id),
                cancellationToken
            );
            if (pathogenDbCount != protocolQuery.PathogenIds.Count)
            {
                logger.LogDebug(
                    "Not all pathogen IDs given exist in database: {detail}",
                    new
                    {
                        TotalIdsGiven = protocolQuery.PathogenIds.Count,
                        TotalFoundInDb = pathogenDbCount,
                    }
                );
                return Result<SearchTreatmentResult>.Failure(
                    new ApplicationError(ApplicationStatus.BadRequest, "Not all pathogen IDs given exist")
                );
            }

            var criteriaDbCount = await context.Criteria.CountAsync(
                c => protocolQuery.CriteriaIds.Contains(c.Id),
                cancellationToken
            );
            if (criteriaDbCount != protocolQuery.CriteriaIds.Count)
            {
                logger.LogDebug(
                    "Not all criteria IDs given exist in database: {detail}",
                    new
                    {
                        TotalIdsGiven = protocolQuery.CriteriaIds.Count,
                        TotalFoundInDb = criteriaDbCount,
                    }
                );
                return Result<SearchTreatmentResult>.Failure(
                    new ApplicationError(ApplicationStatus.BadRequest, "Not all criteria IDs given exist")
                );
            }

            // Empty PathogenIds means no pathogen filter; otherwise the treatment must cover
            // all selected pathogens (treatment.Pathogens ⊆ protocolQuery.PathogenIds).
            var candidates = await context
                .TreatmentProtocols.AsNoTracking()
                .Where(x =>
                    x.Severity == protocolQuery.Severity
                    && x.TreatmentSite == protocolQuery.TreatmentSite
                    && x.Criteria.Select(c => c.Id).All(c => protocolQuery.CriteriaIds.Contains(c))
                )
                .Select(x => new TreatmentCandidate(
                    x.Id,
                    x.Pathogens.Select(p => p.Id).ToList(),
                    x.Criteria.Select(c => c.Id).ToList(),
                    x.Treatments.Select(mc =>
                            mc.Combination.Select(a => new MedicineResult(
                                    a.Id,
                                    a.Name,
                                    a.AntibioticGroup.Name
                                ))
                                .ToList()
                        )
                        .ToList()
                ))
                .ToListAsync(cancellationToken);

            if (protocolQuery.PathogenIds.Count > 0)
            {
                candidates =
                [
                    .. candidates.Where(t => t.PathogenIds.All(p => protocolQuery.PathogenIds.Contains(p))),
                ];
            }

            var matchedCount = candidates.Count;

            // Dominance: hide a narrower treatment when a wider one is applicable
            // (see TreatmentDominance.Filter). e.g. with pathogens [Pseudomonas, S. aureus],
            // the Pseudomonas-only treatment is hidden in favor of the
            // Pseudomonas + S. aureus treatment; if no combo exists, the single stays.
            var survivors = TreatmentDominance.Filter(candidates, protocolQuery.PathogenIds);

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
                "TreatmentProtocol search: severity={Severity}, site={TreatmentSite}, pathogensGiven={PathogenCount}, criteriaGiven={CriteriaCount}, matched={Matched}, dominanceRemoved={DominanceRemoved}, distinctSolutions={DistinctSolutions}",
                protocolQuery.Severity,
                protocolQuery.TreatmentSite,
                protocolQuery.PathogenIds.Count,
                protocolQuery.CriteriaIds.Count,
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
