using Application.Contracts.Data;
using Application.Features.Shared.ManageCriterion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Diseases.GetDiseaseCriteria;

public class GetDiseaseCriteriaHandler(
    IDbContext context,
    IResultMapper<Criterion, CriterionItem> mapper,
    ILogger<GetDiseaseCriteriaHandler> logger)
    : IQueryHandler<GetDiseaseCriteriaQuery, Respira.ServiceDefaults.Contracts.Results.Result<DiseaseCriteriaResult>>
{
    public async Task<Respira.ServiceDefaults.Contracts.Results.Result<DiseaseCriteriaResult>> HandleAsync(GetDiseaseCriteriaQuery query,
        CancellationToken cancellationToken = default)
    {
        // Get disease by ID with all criteria included
        var disease = await context.Diseases
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.IcuHospitalizeCriteria)
            .ThenInclude(x => x.Criterion)
            .Include(x => x.ResistanceRiskFactors)
            .ThenInclude(x => x.Criterion)
            .Include(x => x.EmpiricTreatmentProtocols)
            .ThenInclude(x => x.OtherCriteria)
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken);

        if (disease is null)
        {
            logger.LogDebug("Disease ID not found: {Id}", query.Id);
            return Respira.ServiceDefaults.Contracts.Results.Result<DiseaseCriteriaResult>.Failure(new Error(Status.BadRequest, "Disease ID not found"));
            // throw new NotFoundException(nameof(Disease), query.Id);
        }

        // Since TreatmentProtocol.OtherCriteria can reference to the same criteria as ICU or ResistanceRisk,
        // we need to deduplicate the IDs by using a hash set
        var existingIds = disease.IcuHospitalizeCriteria
            .Select(x => x.Criterion.Id)
            .Concat(disease.ResistanceRiskFactors.Select(x => x.CriterionId))
            .ToHashSet();

        var result = new DiseaseCriteriaResult
        {
            IcuHospitalizeCriteria = disease.IcuHospitalizeCriteria
                .ConvertAll(x => mapper.ToResult(x.Criterion)),
            ResistanceRiskFactorCriteria = disease.ResistanceRiskFactors
                .ConvertAll(x => mapper.ToResult(x.Criterion)),
            OtherCriteria = [.. disease.EmpiricTreatmentProtocols
                .SelectMany(p => p.OtherCriteria) // Flattens the nested lists
                .Where(c => !existingIds.Contains(c.Id))
                .DistinctBy(c => c.Id) // Prevents duplicates if multiple protocols share the same criterion
                .Select(mapper.ToResult)],
        };

        logger.LogDebug("Get disease associated criteria successfully: {result}", new
        {
            IcuCriteriaCount = result.IcuHospitalizeCriteria.Count(),
            RiskFactorCount = result.ResistanceRiskFactorCriteria.Count(),
            OtherCriteriaCount = result.OtherCriteria.Count(),
        });

        return Respira.ServiceDefaults.Contracts.Results.Result<DiseaseCriteriaResult>.Success(Status.Success, result);
    }
}
