using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Diagnosis.GetDiagnoseForm
{
    public class GetDiagnosisFormHandler(IDbContext context, ILogger<GetDiagnosisFormHandler> logger)
        : IQueryHandler<GetDiagnoseFormQuery, Result<DiagnoseFormResult>>
    {
        public async Task<Result<DiagnoseFormResult>> HandleAsync(GetDiagnoseFormQuery query, CancellationToken cancellationToken = default)
        {
            var entities = await context.ClinicalVariables
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var variables = entities
                .OrderBy(x => x.Category)
                .ThenBy(x => x.ValueType)
                .Select(x => new ClinicalVariableResult
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    Code = x.Code,
                    ValueType = x.ValueType,
                    CanonicalUnit = x.CanonicalUnit,
                    IsRequired = x.IsRequired,
                    Category = x.Category,
                    AcceptedValues = (x as CategoricalClinicalVariable)?.AcceptedValues,
                    NormalRange = (x as NumericClinicalVariable)?.NormalRange,
                    AcceptedRange = (x as NumericClinicalVariable)?.AcceptedRange
                })
                .ToList();

            if (variables.Count == 0)
            {
                const string msg = "No clinical variable found";
                logger.LogWarning(msg);
                return Result<DiagnoseFormResult>.Failure(new ApplicationError(ApplicationStatus.BusinessRuleViolation, msg));
            }

            var antibiotics = await context.Antibiotics
                .AsNoTracking()
                .Order()
                .Select(x => new AntibioticResult
                {
                    Id = x.Id,
                    Name = x.Name,
                    AntibioticGroupId = x.AntibioticGroupId,
                    AntibioticGroupName = x.AntibioticGroup.Name
                })
                .ToListAsync();

            var pathogens = await context.Pathogens
                .AsNoTracking()
                .Order()
                .Select(x => new PathogenResult
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync();

            return Result<DiagnoseFormResult>.Success(ApplicationStatus.Success, new DiagnoseFormResult
            {
                ClinicalVariables = variables,
                Antibiotics = antibiotics,
                Pathogens = pathogens
            });
        }
    }
}
