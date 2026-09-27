using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using X.PagedList.EF;

namespace Respira.Clinical.Application.Features.AntibioticGroups.GetPagedAntibioticGroup
{
    public class GetPagedAntibioticGroupHandler(IDbContext context, IPaginationFactory factory)
        : IQueryHandler<GetPagedAntibioticGroupQuery, Result<Pagination<PagedAntibioticGroupItem>>>
    {
        public async Task<Result<Pagination<PagedAntibioticGroupItem>>> HandleAsync(GetPagedAntibioticGroupQuery query, CancellationToken cancellationToken = default)
        {
            // Apply filter
            var queryable = context.AntibioticGroups.AsQueryable();
            if (query.Filter is not null)
            {
                // Search name (contains, case-insensitive)
                if (query.Filter.Name is not null)
                {
                    queryable = queryable.Where(x =>
                        EF.Functions.ILike(x.Name, $"%{query.Filter.Name}%"));
                }

                // Filter by parent ID
                if (query.Filter.ParentId is not null)
                {
                    queryable = queryable.Where(x => x.ParentId == query.Filter.ParentId);
                }
            }

            // Get paged result
            var groups = await queryable
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new PagedAntibioticGroupItem
                {
                    Id = x.Id,
                    Name = x.Name,
                    ParentId = x.ParentId,
                    ParentName = x.Parent == null ? null : x.Parent.Name,
                    Description = x.Description
                })
                .ToPagedListAsync(query.Param.Page, query.Param.Size);

            return Result<Pagination<PagedAntibioticGroupItem>>.Success(ApplicationStatus.Success, factory.Create(groups));
        }
    }
}
