using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Antibiotics.UpdateAntibiotic;

public class UpdateAntibioticHandler(
    IDbContext context,
    IUpdateMapper<Antibiotic, UpdateAntibioticCommand> mapper,
    ILogger<UpdateAntibioticHandler> logger)
    : ICommandHandler<UpdateAntibioticCommand, Result>
{
    public async Task<Result> HandleAsync(UpdateAntibioticCommand command, CancellationToken cancellationToken = default)
    {
        // Check if antibiotic group exists
        var group = await context.AntibioticGroups
            .FirstOrDefaultAsync(x => x.Id == command.AntibioticGroupId, cancellationToken);
        if (group is null)
        {
            logger.LogDebug("Antibiotic group ID not found for antibiotic group: {Id}", command.AntibioticGroupId);
            return Result.Failure(new Error(Status.BadRequest, "Antibiotic group ID not exists"));
        }

        // Get entity by ID
        var antibiotic = await context.Antibiotics
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (antibiotic is null)
        {
            logger.LogDebug("Antibiotic not found: {Id}", command.Id);
            return Result.Failure(new Error(Status.BadRequest, "Antibiotic not found"));
        }

        // Map command to model
        mapper.MapModel(antibiotic, command);

        // Save changes to database
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(Status.Updated);
    }
}
