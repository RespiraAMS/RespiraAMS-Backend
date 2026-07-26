using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Antibiotics.UpdateAntibiotic;

public class UpdateAntibioticHandler(
    IDbContext context,
    IUpdateMapper<Antibiotic, UpdateAntibioticCommand> mapper,
    ILogger<UpdateAntibioticHandler> logger)
    : ICommandHandler<UpdateAntibioticCommand>
{
    public async Task HandleAsync(UpdateAntibioticCommand command, CancellationToken cancellationToken = default)
    {
        // Check if antibiotic group exists
        var group = await context.AntibioticGroups
            .FirstOrDefaultAsync(x => x.Id == command.AntibioticGroupId, cancellationToken);
        if (group is null)
        {
            logger.LogDebug("Antibiotic group ID not found for antibiotic group: {Id}", command.AntibioticGroupId);
            throw new BadRequestException("Antibiotic group ID not exists");
        }

        // Get entity by ID
        var antibiotic = await context.Antibiotics
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (antibiotic is null)
        {
            logger.LogDebug("Antibiotic not found: {Id}", command.Id);
            throw new NotFoundException(nameof(Antibiotic), command.Id);
        }

        // Map command to model
        mapper.MapModel(antibiotic, command);

        // Save changes to database
        if (await context.SaveChangesAsync(cancellationToken) <= 0)
        {
            logger.LogError("Failed to save antibiotic");
            throw new ServerException();
        }
    }
}