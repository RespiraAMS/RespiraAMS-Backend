using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.ClinicalVariables.CreateClinicalVariable
{
    public class CreateClinicalVariableHandler(
        IDbContext context,
        ICreateMapper<CreateClinicalVariableCommand, ClinicalVariable> mapper,
        ILogger<CreateClinicalVariableHandler> logger)
        : ICommandHandler<CreateClinicalVariableCommand, Result<CreateClinicalVariableResult>>
    {
        public async Task<Result<CreateClinicalVariableResult>> HandleAsync(CreateClinicalVariableCommand command, CancellationToken cancellationToken = default)
        {
            // Get the list of all available clinical variables in database
            var variables = await context.ClinicalVariables.ToListAsync(cancellationToken);

            // Force the accepted range and normal range unit to use the canonical unit.
            // We won't strictly throw error here 
            command.AcceptedRange?.Unit = command.CanonicalUnit;
            command.NormalRange?.Unit = command.CanonicalUnit;

            // Because of the recursive structure of Formula, it's hard to check the variable
            // ID as other use cases, so we simply just fetched all the currently available
            // variables, perform mapping and check if any exception is thrown
            var mapResult = mapper.ToModel(command, variables);
            if (mapResult.IsFailure())
            {
                logger.LogDebug("Failed to map command to model: {Error}", mapResult.Error);
                return Result<CreateClinicalVariableResult>.Failure(mapResult.Error!);
            }
            var variable = mapResult.Data!;

            // Check if the normal range is within the accepted range (if numeric clinical variable)
            if (variable.ValueType == ClinicalValueType.Numeric)
            {
                var numericVar = (NumericClinicalVariable)variable;
                if (numericVar.NormalRange?.IsRangeContained(numericVar.AcceptedRange) == false)
                {
                    logger.LogDebug("Normal range is not within the accepted range: {detail}", new
                    {
                        variable.Name,
                        variable.Code,
                        numericVar.NormalRange,
                        numericVar.AcceptedRange,
                    });
                    return Result<CreateClinicalVariableResult>.Failure(new Error(ApplicationStatus.BadRequest, "Normal range is not within the accepted range"));
                }
            }

            // Save to database
            await context.ClinicalVariables.AddAsync(variable, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result<CreateClinicalVariableResult>.Success(ApplicationStatus.Created, new CreateClinicalVariableResult(variable.Id));
        }
    }
}
