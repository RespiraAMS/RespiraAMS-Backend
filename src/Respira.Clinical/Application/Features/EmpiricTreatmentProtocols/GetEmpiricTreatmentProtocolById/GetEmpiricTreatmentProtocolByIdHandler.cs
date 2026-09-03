using Application.Contracts.Data;
using Application.Features.Shared.ManageCriterion;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.EmpiricTreatmentProtocols.GetEmpiricTreatmentProtocolById;

public class GetEmpiricTreatmentProtocolByIdHandler(IDbContext context, IResultMapper<Criterion, CriterionItem> mapper)
    : IQueryHandler<GetEmpiricTreatmentProtocolByIdQuery, Respira.ServiceDefaults.Contracts.Results.Result<EmpiricTreatmentProtocolResult>>
{
    public async Task<Respira.ServiceDefaults.Contracts.Results.Result<EmpiricTreatmentProtocolResult>> HandleAsync(GetEmpiricTreatmentProtocolByIdQuery query,
        CancellationToken cancellationToken = default)
    {
#pragma warning disable RCS1077 // Optimize LINQ method call: ConvertAll won't work with EF Core SQL translation
        var protocol = await context.EmpiricTreatmentProtocols
            .AsNoTracking()
            .AsSplitQuery()
            .Select(x => new EmpiricTreatmentProtocolResult
            {
                Id = x.Id,
                UpdatedAt = x.UpdatedAt,
                Name = x.Name,
                Issuer = x.Issuer,
                IssueDate = x.IssueDate,
                Version = x.Version,
                Severity = x.Severity,
                TreatmentSite = x.TreatmentSite,
                SpecialInfection = x.SpecialInfection == null
                    ? null
                    : new PathogenResult
                    {
                        Id = x.SpecialInfection.Id,
                        Name = x.SpecialInfection.Name,
                    },
                // NOTE: don't translate this using Select(mapper.ToResult) because it will cause
                // an EF SQL translate error
                OtherCriteria = x.OtherCriteria.Select(y => mapper.ToResult(y)).ToList(),
                Medicines = x.Medicines.Select(m => new AntibioticResult
                {
                    Id = m.Id,
                    Name = m.Name
                }).ToList()
            })
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken);
#pragma warning restore RCS1077 // Optimize LINQ method call

        return protocol is null
            ? Respira.ServiceDefaults.Contracts.Results.Result<EmpiricTreatmentProtocolResult>.Failure(new Error(Status.ResourceNotFound, "Empiric treatment protocol with id {id} not found"))
            : Respira.ServiceDefaults.Contracts.Results.Result<EmpiricTreatmentProtocolResult>.Success(Status.Success, protocol);
    }
}
