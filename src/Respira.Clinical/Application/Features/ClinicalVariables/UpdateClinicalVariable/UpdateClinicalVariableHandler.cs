using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.ClinicalVariables.UpdateClinicalVariable
{
    public class UpdateClinicalVariableHandler(
        IDbContext context,
        IUpdateMapper<ClinicalVariable, UpdateClinicalVariableCommand> mapper,
        ILogger<UpdateClinicalVariableHandler> logger)
        : ICommandHandler<UpdateClinicalVariableCommand, Result>
    {
        public async Task<Result> HandleAsync(UpdateClinicalVariableCommand command, CancellationToken cancellationToken = default)
        {
            // Get the list of all available clinical variables in database
            var variables = await context.ClinicalVariables.ToListAsync(cancellationToken);

            // Get the clinical variable from database
            var variable = variables.FirstOrDefault(x => x.Id == command.Id);
            if (variable is null)
            {
                logger.LogDebug("Clinical variable not found: {Id}", command.Id);
                return Result.Failure(new Error(ApplicationStatus.BadRequest, "Clinical variable not found"));
            }

            // Since the ClinicalVariable table used TPH, the value type cannot be changed
            if (command.ValueType != variable.ValueType)
            {
                logger.LogDebug("Command value type is different than the value type, forbid update: {detail}", new
                {
                    command.Id,
                    CommandValueType = command.ValueType,
                    VariableValueType = variable.ValueType,
                });
                return Result.Failure(new Error(
                    ApplicationStatus.BusinessRuleViolation,
                    "Command value type is different than the value type"));
            }


            // Force the accepted range and normal range unit to use the canonical unit.
            // We won't strictly throw error here 
            command.AcceptedRange?.Unit = command.CanonicalUnit;
            command.NormalRange?.Unit = command.CanonicalUnit;

            // Because of the recursive structure of Formula, it's hard to check the variable
            // ID as other use cases, so we simply just fetched all the currently available
            // variables, perform mapping and check if any exception is thrown
            var mapResult = mapper.MapModel(variable, command, variables);
            if (mapResult.IsFailure())
            {
                logger.LogDebug("Failed to map command to model: {Error}", mapResult.Error);
                return Result.Failure(mapResult.Error!);
            }

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
                    return Result.Failure(new Error(ApplicationStatus.BadRequest, "Normal range is not within the accepted range"));
                }
            }

            // Save to database
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(ApplicationStatus.Updated);
        }
    }
}
