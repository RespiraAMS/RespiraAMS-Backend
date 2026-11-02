using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using X.PagedList.EF;

namespace Respira.Clinical.Application.Features.TreatmentProtocols.GetPagedTreatmentProtocol
{
    public class GetPagedTreatmentProtocolHandler(IDbContext context, IPaginationFactory factory)
        : IQueryHandler<GetPagedTreatmentProtocolQuery, Result<Pagination<TreatmentProtocolItem>>>
    {
        public async Task<Result<Pagination<TreatmentProtocolItem>>> HandleAsync(GetPagedTreatmentProtocolQuery query, CancellationToken cancellationToken = default)
        {
            // Apply filter
            var queryable = context.TreatmentProtocols.AsQueryable();
            if (query.Filter is not null)
            {
                if (query.Filter.Severity is not null)
                {
                    queryable = queryable.Where(x => x.Severity == query.Filter.Severity);
                }

                if (query.Filter.TreatmentSite is not null)
                {
                    queryable = queryable.Where(x => x.TreatmentSite == query.Filter.TreatmentSite);
                }

                if (query.Filter.PathogenIds is not null)
                {
                    queryable = queryable.Where(x => x.Pathogens.Select(p => p.Id).Any(p => query.Filter.PathogenIds.Contains(p)));
                }

                if (query.Filter.CriteriaIds is not null)
                {
                    queryable = queryable.Where(x => x.Criteria.Select(c => c.Id).Any(c => query.Filter.CriteriaIds.Contains(c)));
                }
            }

            // Get paged list of treatment protocol
            var protocols = await queryable
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new TreatmentProtocolItem
                {
                    Id = x.Id,
                    Severity = x.Severity,
                    TreatmentSite = x.TreatmentSite,
                    Pathogens = x.Pathogens.Select(p => new PathogenResult(p.Id, p.Name)),
                    Criteria = x.Criteria.Select(c => new CriterionResult(c.Id, c.Name, c.Formula.ToString()!)),
                    Treatments = x.Treatments.Select(t => t.Combination.Select(c => new AntibioticResult
                    {
                        Id = c.Id,
                        Name = c.Name,
                        AntibioticGroupId = c.AntibioticGroupId,
                        AntibioticGroupName = c.AntibioticGroup.Name,
                        Classification = c.Classification
                    }))
                })
                .ToPagedListAsync(query.Param.Page, query.Param.Size, cancellationToken: cancellationToken);

            return Result<Pagination<TreatmentProtocolItem>>.Success(ApplicationStatus.Success, factory.Create(protocols));
        }
    }
}
