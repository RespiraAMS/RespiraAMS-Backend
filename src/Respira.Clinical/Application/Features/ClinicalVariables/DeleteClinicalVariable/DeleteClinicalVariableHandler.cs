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

            // Delete all variables that reference the current variable
            // in their prerequisite formula.
            // Since the Where clause cannot be translated to SQL, we need to
            // load all variables first and then filter them in memory.
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
                foreach (var reference in references)
                {
                    reference.IsDeleted = true;
                    reference.DeletedAt = DateTimeOffset.UtcNow;
                }

                logger.LogDebug(
                    "Deleted {count} variables that reference the current variable {Id}",
                    references.Count,
                    command.Id);
            }, cancellationToken);

            return Result.Success(ApplicationStatus.Deleted);
        }
    }
}
