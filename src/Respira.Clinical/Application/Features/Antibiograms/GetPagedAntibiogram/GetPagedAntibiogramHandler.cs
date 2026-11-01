using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using X.PagedList.EF;

namespace Respira.Clinical.Application.Features.Antibiograms.GetPagedAntibiogram
{
    public class GetPagedAntibiogramHandler(IDbContext context, IPaginationFactory factory)
        : IQueryHandler<GetPagedAntibiogramQuery, Result<Pagination<AntibiogramItem>>>
    {
        public async Task<Result<Pagination<AntibiogramItem>>> HandleAsync(GetPagedAntibiogramQuery query, CancellationToken cancellationToken = default)
        {
            // Apply filter
            var queryable = context.Antibiograms.AsQueryable();
            if (query.Filter is not null)
            {
                if (query.Filter.PathogenId is not null)
                {
                    queryable = queryable.Where(x => x.PathogenId == query.Filter.PathogenId);
                }

                if (query.Filter.MicCategory is not null)
                {
                    queryable = queryable.Where(x => x.MicCategory == query.Filter.MicCategory);
                }
            }

            // Get paged antibiograms
            var antibiograms = await queryable
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new AntibiogramItem
                {
                    Id = x.Id,
                    Pathogen = new PathogenResult(x.Pathogen.Id, x.Pathogen.Name),
                    MicCategory = x.MicCategory,
                    Antibiotics = x.Antibiotics.Select(a => new AntibioticResult
                    {
                        Id = a.Id,
                        Name = a.Name,
                        AntibioticGroupId = a.AntibioticGroupId,
                        AntibioticGroupName = a.AntibioticGroup.Name,
                        Classification = a.Classification
                    })
                })
                .ToPagedListAsync(query.Param.Page, query.Param.Size, cancellationToken: cancellationToken);

            return Result<Pagination<AntibiogramItem>>.Success(ApplicationStatus.Success, factory.Create(antibiograms));
        }
    }
}
