using Application.Contracts.Data;
using Application.Features.Diagnose.Shared;
using Domain.Services.Contracts;
using Domain.Services.Dtos;
using ImTools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Diagnose.TargetedDiagnose;

public class TargetedDiagnoseHandler(
    IDbContext context,
    IDiagnoseService service,
    IMapper<TargetedDiagnoseQuery, PatientInfo> mapper,
    ILogger<TargetedDiagnoseHandler> logger)
    : IQueryHandler<TargetedDiagnoseQuery, TargetedDiagnoseResult>
{
    public async Task<TargetedDiagnoseResult> HandleAsync(TargetedDiagnoseQuery query, CancellationToken cancellationToken = default)
    {
        // Check if pathogen exists
        if (await context.Pathogens.FirstOrDefaultAsync(x => x.Id == query.PathogenId, cancellationToken) is null)
        {
            logger.LogDebug("Pathogen ID not exists: {PathogenId}", query.PathogenId);
            throw new NotFoundException(nameof(Pathogen), query.PathogenId);
        }

        // Get antibiogram for this pathogen
        var antibiogram = await context.Antibiograms
            .AsNoTracking()
            .Include(a => a.FirstPriorityMedicines)
            .ThenInclude(m => m.AntibioticGroup)
            .Include(a => a.FirstPriorityMedicines)
            .ThenInclude(m => m.Dosages)
            .Include(a => a.SecondPriorityMedicines)
            .ThenInclude(m => m.AntibioticGroup)
            .Include(a => a.SecondPriorityMedicines)
            .ThenInclude(m => m.Dosages)
            .FirstOrDefaultAsync(x => x.PathogenId == query.PathogenId, cancellationToken);
        if (antibiogram is null)
        {
            logger.LogDebug("No antibiogram found for this pathogen: {PathogenId}", query.PathogenId);
            throw new UnexpectedException("No antibiogram found for this pathogen");
        }

        var result = service.TargetedDiagnose(mapper.Map(query), antibiogram);
        return new TargetedDiagnoseResult
        {
            Crcl = result.Crcl,
            Medicines = result.Medicines.ConvertAll(m => new AntibioticResult
            {
                Id = m.Id,
                Name = m.Name,
                AntibioticGroupId = m.AntibioticGroupId,
                AntibioticGroupName = m.AntibioticGroup.Name,
                Dose = m.Dosages[0].Dose,
            }),
            Recommendations = [.. antibiogram.FirstPriorityMedicines
                .Append(antibiogram.SecondPriorityMedicines)
                .Select(m => new AntibioticResult
                {
                    Id = m.Id,
                    Name = m.Name,
                    AntibioticGroupId = m.AntibioticGroupId,
                    AntibioticGroupName = m.AntibioticGroup.Name,
                    Dose = m.Dosages[0].Dose,
                })],
        };
    }
}
