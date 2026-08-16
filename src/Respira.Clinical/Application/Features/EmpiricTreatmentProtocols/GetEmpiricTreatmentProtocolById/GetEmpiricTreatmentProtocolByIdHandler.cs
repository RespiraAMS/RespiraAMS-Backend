using Application.Contracts.Data;
using Application.Features.Shared.ManageCriterion;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.EmpiricTreatmentProtocols.GetEmpiricTreatmentProtocolById;

public class GetEmpiricTreatmentProtocolByIdHandler(IDbContext context, IResultMapper<Criterion, CriterionItem> mapper)
    : IQueryHandler<GetEmpiricTreatmentProtocolByIdQuery, EmpiricTreatmentProtocolResult>
{
    public async Task<EmpiricTreatmentProtocolResult> HandleAsync(GetEmpiricTreatmentProtocolByIdQuery query,
        CancellationToken cancellationToken = default)
    {
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
                OtherCriteria = x.OtherCriteria.ConvertAll(mapper.ToResult),
                Medicines = x.Medicines.ConvertAll(m => new AntibioticResult
                {
                    Id = m.Id,
                    Name = m.Name
                })
            })
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken);

        return protocol ?? throw new NotFoundException(nameof(EmpiricTreatmentProtocol), query.Id);
    }
}
