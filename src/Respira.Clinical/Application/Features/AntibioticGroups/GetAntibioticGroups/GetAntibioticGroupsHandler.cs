using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.AntibioticGroups.GetAntibioticGroups;

public class GetAntibioticGroupsHandler(IDbContext context)
    : IQueryHandler<GetAntibioticGroupQuery, ICollection<GetAntibioticGroupResult>>
{
    public async Task<ICollection<GetAntibioticGroupResult>> HandleAsync(GetAntibioticGroupQuery query,
        CancellationToken cancellationToken = default)
    {
        return await context.AntibioticGroups
            .Select(x => new GetAntibioticGroupResult
            {
                Id = x.Id,
                Name = x.Name,
            })
            .ToListAsync(cancellationToken);
    }
}