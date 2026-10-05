using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using X.PagedList.EF;

namespace Respira.Clinical.Application.Features.Pathogens.GetPagedPathogen
{
    public class GetPagedPathogensHandler(IDbContext context, IPaginationFactory factory)
        : IQueryHandler<GetPagedPathogenQuery, Result<Pagination<PagedPathogenItem>>>
    {
        public async Task<Result<Pagination<PagedPathogenItem>>> HandleAsync(GetPagedPathogenQuery query, CancellationToken cancellationToken = default)
        {
            // Apply filter
            var queryable = context.Pathogens.AsQueryable();
            if (query.Filter is not null)
            {
                if (query.Filter.Name is not null)
                {
                    queryable = queryable.Where(x => EF.Functions.ILike(x.Name, $"%{query.Filter.Name}%"));
                }
            }

            // Get paged pathogen
            var pathogens = await queryable
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new PagedPathogenItem()
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    IsAtypical = x.IsAtypical
                })
                .ToPagedListAsync(query.Param.Page, query.Param.Size);
            return Result<Pagination<PagedPathogenItem>>.Success(ApplicationStatus.Success, factory.Create(pathogens));
        }
    }
}
