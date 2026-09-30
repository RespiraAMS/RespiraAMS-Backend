using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using X.PagedList.EF;

namespace Respira.Clinical.Application.Features.SuspectedCauses.GetPagedSuspectedCause
{
    public class GetPagedSuspectedCauseHandler(IDbContext context, IPaginationFactory factory)
        : IQueryHandler<GetPagedSuspectedCauseQuery, Result<Pagination<PagedSuspectedCauseItem>>>
    {
        public async Task<Result<Pagination<PagedSuspectedCauseItem>>> HandleAsync(GetPagedSuspectedCauseQuery query, CancellationToken cancellationToken = default)
        {
            // Apply filter
            var queryable = context.SuspectedCauses.AsQueryable();
            if (query.Filter is not null)
            {
                // Filter by pathogen ID
                if (query.Filter.PathogenId is not null)
                {
                    queryable = queryable.Where(x => x.PathogenId == query.Filter.PathogenId);
                }

                // Filter by severity
                if (query.Filter.Severity is not null)
                {
                    queryable = queryable.Where(x => x.Severity == query.Filter.Severity);
                }

                // Filter by treatment site
                if (query.Filter.TreatmentSite is not null)
                {
                    queryable = queryable.Where(x => x.TreatmentSite == query.Filter.TreatmentSite);
                }
            }

            // Get paged result
            var causes = await queryable
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new PagedSuspectedCauseItem
                {
                    Id = x.Id,
                    PathogenId = x.PathogenId,
                    PathogenName = x.Pathogen.Name,
                    Severity = x.Severity,
                    TreatmentSite = x.TreatmentSite,
                })
                .ToPagedListAsync(query.Param.Page, query.Param.Size);

            return Result<Pagination<PagedSuspectedCauseItem>>.Success(ApplicationStatus.Success, factory.Create(causes));
        }
    }
}
