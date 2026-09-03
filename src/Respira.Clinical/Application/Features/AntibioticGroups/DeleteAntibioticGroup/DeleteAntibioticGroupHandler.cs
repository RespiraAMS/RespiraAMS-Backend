using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.AntibioticGroups.DeleteAntibioticGroup;

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
            return Result.Failure(new Error(Status.BadRequest, "Antibiotic group not found"));
        }

        // Delete antibiotic group - cascade delete all associating antibiotic
        await context.ExecuteInTransactionAsync(async () =>
        {
            // Soft delete antibiotic group
            group.IsDeleted = true;
            group.DeletedAt = DateTimeOffset.UtcNow;

            // Cascade delete antibiotic
            var count = await context.Antibiotics
                .Where(x => x.AntibioticGroupId == command.Id)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(a => a.IsDeleted, true)
                    .SetProperty(a => a.DeletedAt, DateTimeOffset.UtcNow), cancellationToken);
            logger.LogDebug("Cascade delete {count} antibiotics when delete antibiotic group {Id}", count, command.Id);
        }, cancellationToken);
        return Result.Success(Status.Deleted);
    }
}
