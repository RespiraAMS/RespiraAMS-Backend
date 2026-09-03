using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.AntibioticGroups.GetAntibioticGroups;

public class GetAntibioticGroupsHandler(IDbContext context)
    : IQueryHandler<GetAntibioticGroupsQuery, Result<GetAntibioticGroupsResult>>
{
    public async Task<Result<GetAntibioticGroupsResult>> HandleAsync(GetAntibioticGroupsQuery query, CancellationToken cancellationToken = default)
    {
        var group = await context.AntibioticGroups
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new AntibioticGroupItem()
            {
                Id = x.Id,
                Name = x.Name,
            })
            .ToListAsync(cancellationToken);
        return Result<GetAntibioticGroupsResult>.Success(Status.Success, new GetAntibioticGroupsResult { AntibioticGroups = group });
    }
}
