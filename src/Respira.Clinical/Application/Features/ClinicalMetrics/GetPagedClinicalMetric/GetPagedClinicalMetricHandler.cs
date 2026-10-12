using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using X.PagedList.EF;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.GetPagedClinicalMetric
{
    public class GetPagedClinicalMetricHandler(IDbContext context, IPaginationFactory factory)
        : IQueryHandler<GetPagedClinicalMetricQuery, Result<Pagination<PagedClinicalMetricItem>>>
    {
        public async Task<Result<Pagination<PagedClinicalMetricItem>>> HandleAsync(GetPagedClinicalMetricQuery query, CancellationToken cancellationToken = default)
        {
            // Apply filter
            var queryable = context.ClinicalMetrics.AsQueryable();
            if (query.Filter is not null)
            {
                if (query.Filter.Code is not null)
                {
                    queryable = queryable.Where(x => x.Code.Equals(query.Filter.Code));
                }

                if (query.Filter.IsApplied is not null)
                {
                    queryable = queryable.Where(x => x.IsApplied == query.Filter.IsApplied);
                }
            }

            var metrics = await queryable
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new PagedClinicalMetricItem
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    Description = x.Description,
                    Version = x.Version,
                    Publisher = x.Publisher,
                    IsApplied = x.IsApplied,
                })
                .ToPagedListAsync(query.Param.Page, query.Param.Size);

            return Result<Pagination<PagedClinicalMetricItem>>.Success(ApplicationStatus.Success, factory.Create(metrics));
        }
    }
}
