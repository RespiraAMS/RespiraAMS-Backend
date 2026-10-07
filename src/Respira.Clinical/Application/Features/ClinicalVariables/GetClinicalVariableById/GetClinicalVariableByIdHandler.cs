using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.ClinicalVariables.GetClinicalVariableById
{
    public class GetClinicalVariableByIdHandler(IDbContext context)
        : IQueryHandler<GetClinicalVariableByIdQuery, Result<ClinicalVariableResult>>
    {
        public async Task<Result<ClinicalVariableResult>> HandleAsync(GetClinicalVariableByIdQuery query, CancellationToken cancellationToken = default)
        {
            var variable = await context.ClinicalVariables
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken);

            if (variable is null)
            {
                return Result<ClinicalVariableResult>.Failure(new Error(ApplicationStatus.ResourceNotFound, "Clinical variable not found"));
            }

            // Map entity to result object
            var result = new ClinicalVariableResult
            {
                Id = variable.Id,
                Name = variable.Name,
                Code = variable.Code,
                Description = variable.Description,
                ValueType = variable.ValueType,
                CanonicalUnit = variable.CanonicalUnit,
                IsRequired = variable.IsRequired,
                Category = variable.Category,
                Prerequisite = variable.Prerequisite?.ToString(),
            };

            if (variable is NumericClinicalVariable numericVariable)
            {
                result.AcceptedRange = numericVariable.AcceptedRange;
                result.NormalRange = numericVariable.NormalRange;
            }

            if (variable is CategoricalClinicalVariable categoricalVariable)
            {
                result.AcceptedValues = categoricalVariable.AcceptedValues;
            }

            return Result<ClinicalVariableResult>.Success(ApplicationStatus.Success, result);
        }
    }
}
