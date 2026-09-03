using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Antibiotics.UpdateAntibiotic;

public class UpdateAntibioticHandler(
    IDbContext context,
    IUpdateMapper<Antibiotic, UpdateAntibioticCommand> mapper,
    ILogger<UpdateAntibioticHandler> logger)
    : ICommandHandler<UpdateAntibioticCommand, Respira.ServiceDefaults.Contracts.Results.Result>
{
    public async Task<Respira.ServiceDefaults.Contracts.Results.Result> HandleAsync(UpdateAntibioticCommand command, CancellationToken cancellationToken = default)
    {
        // Check if antibiotic group exists
        var group = await context.AntibioticGroups
            .FirstOrDefaultAsync(x => x.Id == command.AntibioticGroupId, cancellationToken);
        if (group is null)
        {
            logger.LogDebug("Antibiotic group ID not found for antibiotic group: {Id}", command.AntibioticGroupId);
            return Respira.ServiceDefaults.Contracts.Results.Result.Failure(new Error(Status.BadRequest, "Antibiotic group ID not exists"));
            // throw new BadRequestException("Antibiotic group ID not exists");
        }

        // Get entity by ID
        var antibiotic = await context.Antibiotics
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (antibiotic is null)
        {
            logger.LogDebug("Antibiotic not found: {Id}", command.Id);
            return Respira.ServiceDefaults.Contracts.Results.Result.Failure(new Error(Status.BadRequest, "Antibiotic not found"));
            // throw new NotFoundException(nameof(Antibiotic), command.Id);
        }

        // Map command to model
        mapper.MapModel(antibiotic, command);

        // Save changes to database
        await context.SaveChangesAsync(cancellationToken);
        return Respira.ServiceDefaults.Contracts.Results.Result.Success(Status.Updated);
    }
}
