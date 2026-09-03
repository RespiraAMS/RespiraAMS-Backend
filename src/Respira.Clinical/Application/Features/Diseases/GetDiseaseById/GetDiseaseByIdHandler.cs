using Application.Contracts.Data;
using Application.Features.Shared.ManageCriterion;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Diseases.GetDiseaseById;

public class GetDiseaseByIdHandler(IDbContext context, IResultMapper<Criterion, CriterionItem> mapper)
    : IQueryHandler<GetDiseaseByIdQuery, Result<DiseaseResult>>
{
    public async Task<Result<DiseaseResult>> HandleAsync(GetDiseaseByIdQuery query, CancellationToken cancellationToken = default)
    {
        var disease = await context.Diseases
            .AsNoTracking()
            .AsSplitQuery()
            .Select(x => new DiseaseResult
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                IcuScoreThreshold = x.IcuScoreThreshold,
                IcuHospitalizeCriteria = x.IcuHospitalizeCriteria
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new IcuHospitalizeCriterionResult
                    {
                        Id = c.Id,
                        Criterion = mapper.ToResult(c.Criterion), // Safe to use since this is first level projection
                        Score = c.Score
                    })
                    .ToList(),
                ResistanceRiskFactors = x.ResistanceRiskFactors
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(f => new ResistanceRiskFactorResult
                    {
                        Id = f.Id,
                        PathogenName = f.Pathogen.Name,
                        Criterion = mapper.ToResult(f.Criterion),
                        Name = f.Name
                    })
                    .ToList(),
                Causes = x.Causes
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new CauseResult
                    {
                        Id = c.Id,
                        PathogenName = c.Pathogen.Name,
                        Severity = c.Severity,
                        TreatmentSite = c.TreatmentSite,
                    })
                    .ToList(),
                EmpiricTreatmentProtocols = x.EmpiricTreatmentProtocols
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(e => new EmpiricTreatmentProtocolResult
                    {
                        Id = e.Id,
                        UpdatedAt = e.UpdatedAt,
                        Name = e.Name,
                        Issuer = e.Issuer,
                        IssueDate = e.IssueDate,
                        Version = e.Version
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken);

        return disease is null
            ? Result<DiseaseResult>.Failure(new Error(Status.ResourceNotFound, "Disease ID not found"))
            : Result<DiseaseResult>.Success(Status.Success, disease);
    }
}
