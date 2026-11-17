using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Domain.Services;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Diagnosis.WarnAntibioticCombination
{
    /*
     * NOTE: this is not a feature, this is simply needed for a UI warning
     * when picking a custom combination that has antibiotics of same group.
     * This is unsual, but in some really rare cases, it still acceptable,
     * so it's not a straight out reject, but a warning. Since the rule is
     * complex, client need to call server side instead of implementing their own
     */

    public class WarnAntibioticCombinationHandler(
        IDbContext context,
        IDiagnoseService service,
        ILogger<WarnAntibioticCombinationHandler> logger)
        : IQueryHandler<WarnAntibioticCombinationQuery, Result<WarnAntibioticCombinationResult>>
    {
        public async Task<Result<WarnAntibioticCombinationResult>> HandleAsync(WarnAntibioticCombinationQuery query, CancellationToken cancellationToken = default)
        {
            // Load clinical context
            var clinicalContext = new ClinicalContext
            {
                AntibioticGroups = await context.AntibioticGroups
                    .Include(x => x.Parent)
                    .ToListAsync(cancellationToken),
                Antibiograms = [],
                Antibiotics = await context.Antibiotics
                    .Include(x => x.AntibioticGroup)
                    .ToListAsync(cancellationToken),
                Metrics = [],
                Pathogens = [],
                SuspectedCauses = [],
                TreatmentProtocols = [],
                Variables = [],
            };

            var medicines = clinicalContext.Antibiotics
                .Where(x => query.AntibioticIds.Contains(x.Id))
                .ToList();
            if (medicines.Count != query.AntibioticIds.Count)
            {
                logger.LogDebug("Not all antibiotic IDs exist in database: {Detail}", new
                {
                    AntibioticDbCount = medicines.Count,
                    AntibioticProvidedCount = query.AntibioticIds.Count
                });
                return Result<WarnAntibioticCombinationResult>.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Not all medicine IDs exist in database"));
            }

            // Check if the medicine that doctor chosen is valid.
            // A valid treatment medicine must not violate rule:
            // all antibiotics in a combination must not belong to a group
            var result = service.IsMedicineCombinationValid(clinicalContext, medicines);
            if (result.IsFailure())
            {
                logger.LogDebug("Failed to validate medicine combination: {Error}", result.Error);
                return Result<WarnAntibioticCombinationResult>.Failure(result.Error!);
            }

            return Result<WarnAntibioticCombinationResult>.Success(
                ApplicationStatus.Success,
                new WarnAntibioticCombinationResult(!result.Data));
        }
    }
}
