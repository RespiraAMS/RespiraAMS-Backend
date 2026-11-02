using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Domain.Entities;
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
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Clinical variable not found"));
            }

            // Business rule: if this variable is being used somewhere else,
            // forbid delete to keep data integrity
            var variables = await context.ClinicalVariables.ToListAsync(cancellationToken);
            if (variables.Any(x => x.Prerequisite?.Variables.Select(v => v.Id).Contains(command.Id) == true))
            {
                return Result.Failure(new ApplicationError(
                    ApplicationStatus.BusinessRuleViolation,
                    "This variable is being used in other prerequisite formula, cannot delete"));
            }

            var criteria = await context.Criteria.ToListAsync(cancellationToken);
            if (criteria.Any(x => x.Formula.Variables.Select(v => v.Id).Contains(command.Id)))
            {
                return Result.Failure(new ApplicationError(
                    ApplicationStatus.BusinessRuleViolation,
                    "This variable is being used in other criterion formula, cannot delete"));
            }

            var rules = await context.MetricRules.ToListAsync(cancellationToken);
            if (rules.Any(x => x is ScoringRule sr && sr.ScoreFunction.Variables.Select(v => v.Id).Contains(command.Id)))
            {
                return Result.Failure(new ApplicationError(
                    ApplicationStatus.BusinessRuleViolation,
                    "This variable is being used in other scoring rule score function formula, cannot delete"));
            }


            // Delete the variable itself
            variable.IsDeleted = true;
            variable.DeletedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(ApplicationStatus.Deleted);
        }
    }
}
