using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.AntibioticGroups.UpdateAntibioticGroup;

public class UpdateAntibioticGroupHandler(
    IDbContext context,
    IUpdateMapper<AntibioticGroup, UpdateAntibioticGroupCommand> mapper,
    ILogger<UpdateAntibioticGroupHandler> logger) : ICommandHandler<UpdateAntibioticGroupCommand>
{
    public async Task HandleAsync(UpdateAntibioticGroupCommand command, CancellationToken cancellationToken = default)
    {
        // Check if parent ID exists if provided
        if (command.ParentId is not null)
        {
            var parent = await context.AntibioticGroups
                .FirstOrDefaultAsync(x => x.Id == command.ParentId, cancellationToken);
            if (parent is null)
            {
                logger.LogDebug("Parent ID not found for antibiotic group: {Id}", command.ParentId);
                throw new BadRequestException("Parent ID not exists");
            }
        }

        // Get entity from database
        var group = await context.AntibioticGroups
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (group is null)
        {
            logger.LogDebug("Antibiotic group not found: {Id}", command.Id);
            throw new NotFoundException(nameof(AntibioticGroup), command.Id);
        }

        // Map from command to model
        mapper.MapModel(group, command);

        // Save changes to database
        if (await context.SaveChangesAsync(cancellationToken) <= 0)
        {
            logger.LogError("Failed to update antibiotic group to database: {Id}", command.Id);
            throw new ServerException();
        }
    }
}