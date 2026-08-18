using Application.Contracts.Data;
using Domain.Services.Contracts;
using Domain.Services.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Diagnose;

public class DiagnoseHandler(
    IDbContext context,
    IDiagnoseService service,
    ICreateMapper<ClinicalPicture, DiagnoseQuery> mapper,
    ILogger<DiagnoseHandler> logger)
    : IQueryHandler<DiagnoseQuery, DiagnoseResult>
{
    public async Task<DiagnoseResult> HandleAsync(DiagnoseQuery query, CancellationToken cancellationToken = default)
    {
        // Get disease
        var disease = await context.Diseases
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.IcuHospitalizeCriteria)
            .ThenInclude(i => i.Criterion)
            .Include(x => x.ResistanceRiskFactors)
            .ThenInclude(r => r.Criterion)
            .Include(x => x.ResistanceRiskFactors)
            .ThenInclude(r => r.Pathogen)
            .Include(x => x.EmpiricTreatmentProtocols)
            .ThenInclude(e => e.SpecialInfection)
            .Include(x => x.EmpiricTreatmentProtocols)
            .ThenInclude(e => e.Medicines)
            .FirstOrDefaultAsync(x => x.Id == query.DiseaseId, cancellationToken);

        if (disease is null)
        {
            logger.LogDebug("Disease not found: {Id}", query.DiseaseId);
            throw new NotFoundException(nameof(Disease), query.DiseaseId);
        }

        // Check clinical picture criteria IDs all exist in this disease
        if (!query.IcuHospitalizeCriteria.All(x =>
                disease.IcuHospitalizeCriteria.Select(icu => icu.CriterionId).Contains(x)))
        {
            logger.LogWarning("Not all ICU hospitalize criteria ID exist");
            throw new BadRequestException("Not all ICU hospitalize criteria ID exist");
        }

        if (!query.ResistanceRiskFactors.All(x =>
                disease.ResistanceRiskFactors.Select(risk => risk.CriterionId).Contains(x)))
        {
            logger.LogWarning("Not all resistance risk factors ID exist");
            throw new BadRequestException("Not all resistance risk factors ID exist");
        }

        if (await context.Criteria.CountAsync(x => query.OtherCriteria.Contains(x.Id), cancellationToken) !=
            query.OtherCriteria.Count)
        {
            logger.LogWarning("Not all other criteria IDs exists");
            throw new BadRequestException("Not all other criteria IDs exists");
        }

        // Extract clinical picture
        var clinicalPicture = mapper.ToModel(query);

        // return service.Diagnose(disease, clinicalPicture);
        return new()
        {
            Medicines = [],
        };
    }
}
