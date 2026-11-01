using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.AntibioticGroups.DeleteAntibioticGroup
{
    public class DeleteAntibioticGroupHandler(IDbContext context, ILogger<DeleteAntibioticGroupHandler> logger)
        : ICommandHandler<DeleteAntibioticGroupCommand, Result>
    {
        public async Task<Result> HandleAsync(DeleteAntibioticGroupCommand command, CancellationToken cancellationToken = default)
        {
            // Get entity by ID
            var group = await context.AntibioticGroups
                .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
            if (group is null)
            {
                logger.LogDebug("Antibiotic group not found: {Id}", command.Id);
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Antibiotic group not found"));
            }

            // Business rule: if this antibiotic group is used by either other group,
            // or antibiotic, we FORBID deleting antibiotic group to preserve data integrity
            if (await context.AntibioticGroups.AnyAsync(x => x.ParentId == group.Id, cancellationToken))
            {
                return Result.Failure(new ApplicationError(
                    ApplicationStatus.BusinessRuleViolation,
                    "This antibiotic group is referenced by other group, cannot delete"));
            }

            if (await context.Antibiotics.AnyAsync(x => x.AntibioticGroupId == group.Id, cancellationToken))
            {
                return Result.Failure(new ApplicationError(
                    ApplicationStatus.BusinessRuleViolation,
                    "This antibiotic group is referenced by other antibiotic, cannot delete"));
            }

            // Soft delete antibiotic group
            group.IsDeleted = true;
            group.DeletedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(ApplicationStatus.Deleted);
        }
    }
}
