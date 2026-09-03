using Application.Contracts.Data;
using Application.Features.Diagnose.Shared;
using Domain.Services.Contracts;
using Domain.Services.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Diagnose.EmpiricalDiagnose;

public class EmpiricalDiagnoseHandler
(
    IDbContext context,
    IDiagnoseService service,
    IMapper<EmpiricalDiagnoseQuery, PatientInfo> patientInfoMapper,
    IMapper<EmpiricalDiagnoseQuery, ClinicalPicture> clinicalPictureMapper,
    ILogger<EmpiricalDiagnoseHandler> logger)
    : IQueryHandler<EmpiricalDiagnoseQuery, Result<EmpiricalDiagnoseResult>>
{
    public async Task<Result<EmpiricalDiagnoseResult>> HandleAsync(EmpiricalDiagnoseQuery query, CancellationToken cancellationToken = default)
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
            .ThenInclude(m => m.Dosages)
            .Include(x => x.EmpiricTreatmentProtocols)
            .ThenInclude(e => e.Medicines)
            .ThenInclude(m => m.AntibioticGroup)
            .FirstOrDefaultAsync(x => x.Id == query.DiseaseId, cancellationToken);

        if (disease is null)
        {
            logger.LogDebug("Disease not found: {Id}", query.DiseaseId);
            return Result<EmpiricalDiagnoseResult>.Failure(new Error(Status.ResourceNotFound, "Disease not found"));
        }

        // Check clinical picture criteria IDs all exist in this disease
        if (!query.IcuHospitalizeCriteria.All(x => disease.IcuHospitalizeCriteria.Select(icu => icu.CriterionId).Contains(x)))
        {
            logger.LogWarning("Not all ICU hospitalize criteria ID exist");
            return Result<EmpiricalDiagnoseResult>.Failure(new Error(Status.BadRequest, "Not all ICU hospitalize criteria ID exist"));
        }

        if (!query.ResistanceRiskFactors.All(x => disease.ResistanceRiskFactors.Select(risk => risk.CriterionId).Contains(x)))
        {
            logger.LogWarning("Not all resistance risk factors ID exist");
            return Result<EmpiricalDiagnoseResult>.Failure(new Error(Status.BadRequest, "Not all resistance risk factors ID exist"));
        }

        if (await context.Criteria.CountAsync(x => query.OtherCriteria.Contains(x.Id), cancellationToken) != query.OtherCriteria.Count)
        {
            logger.LogWarning("Not all other criteria IDs exists");
            return Result<EmpiricalDiagnoseResult>.Failure(new Error(Status.BadRequest, "Not all other criteria IDs exists"));
        }

        // Map from query to DTOs
        var info = patientInfoMapper.Map(query);
        var picture = clinicalPictureMapper.Map(query);

        // Diagnose
        var result = service.EmpiricalDiagnose(disease, info, picture);
        if (result.IsFailure())
        {
            logger.LogDebug("Diagnose service failed: {Error}", result.Error);
            return Result<EmpiricalDiagnoseResult>.Failure(result.Error!);
        }
        var diagnosis = (Domain.Services.Dtos.EmpiricalDiagnoseResult)result.Data!;

        // Merge all the medicines in all recommendations protocols into a single list
        var recommendations = new List<Antibiotic>();
        diagnosis.References.ForEach(r => recommendations.AddRange(r.Medicines));

        return Result<EmpiricalDiagnoseResult>.Success(Status.Success, new EmpiricalDiagnoseResult
        {
            Crcl = diagnosis.Crcl,
            Medicines = diagnosis.Medicines.ConvertAll(m => new AntibioticResult
            {
                Id = m.Id,
                Name = m.Name,
                AntibioticGroupId = m.AntibioticGroupId,
                AntibioticGroupName = m.AntibioticGroup.Name,
                Classification = m.Classification,
                Dosages = m.Dosages.ConvertAll(d => new DosageResult
                {
                    RouteOfAdministration = d.RouteOfAdministration,
                    Dose = d.Dose,
                }),
            }),
            Recommendations = recommendations.ConvertAll(r => new AntibioticResult
            {
                Id = r.Id,
                Name = r.Name,
                AntibioticGroupId = r.AntibioticGroupId,
                AntibioticGroupName = r.AntibioticGroup.Name,
                Classification = r.Classification,
                Dosages = r.Dosages.ConvertAll(d => new DosageResult
                {
                    RouteOfAdministration = d.RouteOfAdministration,
                    Dose = d.Dose,
                }),
            }),
            Severity = diagnosis.Severity,
            TreatmentSite = diagnosis.TreatmentSite,
            InfectionProbabilities = diagnosis.InfectionProbabilities.ConvertAll(p => new InfectionProbability
            {
                PathogenId = p.Pathogen.Id,
                PathogenName = p.Pathogen.Name,
                Probability = p.Probability,
            }),
            References = diagnosis.References.ConvertAll(r => new EmpiricalTreatmentProtocolResult
            {
                Id = r.Id,
                UpdatedAt = r.UpdatedAt,
                Name = r.Name,
                IssueDate = r.IssueDate,
                Issuer = r.Issuer,
                Version = r.Version
            })
        });
    }
}
