using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Application.Features.Diagnose.EmpiricalDiagnose.GetDiagnoseForm
{
    public class GetDiagnoseFormHandler(IDbContext context, ILogger<GetDiagnoseFormHandler> logger)
        : IQueryHandler<GetDiagnoseFormQuery, Result<GetDiagnoseFormResult>>
    {
        public async Task<Result<GetDiagnoseFormResult>> HandleAsync(GetDiagnoseFormQuery query, CancellationToken cancellationToken = default)
        {
            var variables = await context.ClinicalVariables
                .AsNoTracking()
                .Select(x => new ClinicalVariableResult
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    Code = x.Code,
                    ValueType = x.ValueType,
                    CanonicalUnit = x.CanonicalUnit
                })
                .ToListAsync(cancellationToken);

            if (variables.Count == 0)
            {
                const string msg = "No clinical variable found";
                logger.LogWarning(msg);
                return Result<GetDiagnoseFormResult>.Failure(new Error(ApplicationStatus.BusinessRuleViolation, msg));
            }

            return Result<GetDiagnoseFormResult>.Success(ApplicationStatus.Success, new GetDiagnoseFormResult
            {
                Variables = variables.Select(x => new ClinicalVariableResult
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    Code = x.Code,
                    ValueType = x.ValueType,
                    CanonicalUnit = x.CanonicalUnit
                })
            });
        }
    }
}
