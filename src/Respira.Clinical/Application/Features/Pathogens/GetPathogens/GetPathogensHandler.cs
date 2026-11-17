using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Pathogens.GetPathogens
{
    public class GetPathogensHandler(IDbContext context) : IQueryHandler<GetPathogensQuery, Result<GetPathogensResult>>
    {
        public async Task<Result<GetPathogensResult>> HandleAsync(GetPathogensQuery query,
            CancellationToken cancellationToken = default)
        {
            var pathogens = await context.Pathogens
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new PathogenItem()
                {
                    Id = x.Id,
                    Name = x.Name,
                })
                .ToListAsync(cancellationToken);
            return Result<GetPathogensResult>.Success(ApplicationStatus.Success, new GetPathogensResult { Pathogens = pathogens });
        }
    }
}
