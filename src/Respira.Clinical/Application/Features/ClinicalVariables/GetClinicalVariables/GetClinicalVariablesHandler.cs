using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.ClinicalVariables.GetClinicalVariables
{
    public class GetClinicalVariablesHandler(IDbContext context)
        : IQueryHandler<GetClinicalVariablesQuery, Result<GetClinicalVariablesResult>>
    {
        public async Task<Result<GetClinicalVariablesResult>> HandleAsync(GetClinicalVariablesQuery query, CancellationToken cancellationToken = default)
        {
            var variables = await context.ClinicalVariables
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new ClinicalVariableItem(x.Id, x.Name, x.Code))
                .ToListAsync(cancellationToken);
            return Result<GetClinicalVariablesResult>.Success(
                ApplicationStatus.Success,
                new GetClinicalVariablesResult(variables));
        }
    }
}
