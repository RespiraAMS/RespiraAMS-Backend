using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.ClinicalVariables.DeleteClinicalVariable
{
    public class DeleteClinicalVariableHandler(IDbContext context, ILogger<DeleteClinicalVariableHandler> logger)
        : ICommandHandler<DeleteClinicalVariableCommand, Result>
    {
        public async Task<Result> HandleAsync(DeleteClinicalVariableCommand command, CancellationToken cancellationToken = default)
        {
            // Get the clinical variable from database
            var variable = await context.ClinicalVariables.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
            if (variable is null)
            {
                logger.LogDebug("Clinical variable not found: {Id}", command.Id);
                return Result.Failure(new Error(ApplicationStatus.BadRequest, "Clinical variable not found"));
            }

            // Find all variables that have their prerequisite formula referencing the current variable,
            // and set it to null. 
            // NOTE:, we won't cascade delete, because the referenced variables can still exist
            // independently (for example, the FEMALE variable deleted shouldn't make the
            // PREGNANT-OR-LACTATING variable meaningless. Prerequisite formulas are only used
            // to ensure that patient symptoms are consistent/make sense, not their existing 
            // criteria
            var variables = await context.ClinicalVariables.ToListAsync(cancellationToken);
            var references = variables
                .Where(x => x.Prerequisite?.Variables.Select(v => v.Id).Contains(command.Id) == true)
                .ToList();

            // Delete variables in a whole transaction 
            await context.ExecuteInTransactionAsync(async () =>
            {
                // Delete the variable itself
                variable.IsDeleted = true;
                variable.DeletedAt = DateTimeOffset.UtcNow;

                // Delete all variables that reference the current variable
                // in their prerequisite formula
                references.ForEach(r => r.Prerequisite = null);
                logger.LogDebug(
                    "Deleted {count} variables' prerequisite formula that reference the current variable {Id}",
                    references.Count,
                    command.Id);
            }, cancellationToken);

            return Result.Success(ApplicationStatus.Deleted);
        }
    }
}
