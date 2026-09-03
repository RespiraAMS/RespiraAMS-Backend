using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.AntibioticGroups.UpdateAntibioticGroup;

public class UpdateAntibioticGroupHandler(
    IDbContext context,
    IUpdateMapper<AntibioticGroup, UpdateAntibioticGroupCommand> mapper,
    ILogger<UpdateAntibioticGroupHandler> logger) : ICommandHandler<UpdateAntibioticGroupCommand, Respira.ServiceDefaults.Contracts.Results.Result>
{
    public async Task<Result> HandleAsync(UpdateAntibioticGroupCommand command, CancellationToken cancellationToken = default)
    {
        // Check if parent ID exists if provided
        if (command.ParentId is not null)
        {
            var parent = await context.AntibioticGroups
                .FirstOrDefaultAsync(x => x.Id == command.ParentId, cancellationToken);
            if (parent is null)
            {
                logger.LogDebug("Parent ID not found for antibiotic group: {Id}", command.ParentId);
                return Result.Failure(new Error(Status.BadRequest, "Antibiotic group parent ID not found"));
            }
        }

        // Get entity from database
        var group = await context.AntibioticGroups
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (group is null)
        {
            logger.LogDebug("Antibiotic group not found: {Id}", command.Id);
            return Result.Failure(new Error(Status.BadRequest, "Antibiotic group not found"));
        }

        // Map from command to model
        mapper.MapModel(group, command);

        // Save changes to database
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(Status.Updated);
    }
}
