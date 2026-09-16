using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Application.Features.Diagnosis.EmpiricalDiagnosis.GetDiagnosisForm
{
    public class GetDiagnosisFormHandler(IDbContext context, ILogger<GetDiagnosisFormHandler> logger)
        : IQueryHandler<GetDiagnosisFormQuery, Result<GetDiagnosisFormResult>>
    {
        public async Task<Result<GetDiagnosisFormResult>> HandleAsync(GetDiagnosisFormQuery query, CancellationToken cancellationToken = default)
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
                return Result<GetDiagnosisFormResult>.Failure(new Error(ApplicationStatus.BusinessRuleViolation, msg));
            }

            return Result<GetDiagnosisFormResult>.Success(ApplicationStatus.Success, new GetDiagnosisFormResult
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
