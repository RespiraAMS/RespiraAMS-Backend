using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Pathogens.UpdatePathogen;

public class UpdatePathogenHandler(
    IDbContext context,
    IUpdateMapper<Pathogen, UpdatePathogenCommand> mapper,
    ILogger<UpdatePathogenHandler> logger)
    : ICommandHandler<UpdatePathogenCommand, Respira.ServiceDefaults.Contracts.Results.Result>
{
    public async Task<Respira.ServiceDefaults.Contracts.Results.Result> HandleAsync(UpdatePathogenCommand command, CancellationToken cancellationToken = default)
    {
        // Get pathogen by ID
        var pathogen = await context.Pathogens
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (pathogen is null)
        {
            logger.LogWarning("Pathogen ID not found");
            return Respira.ServiceDefaults.Contracts.Results.Result.Failure(new Error(Status.BadRequest, "Pathogen ID not found"));
            // throw new NotFoundException(nameof(Pathogen), command.Id);
        }

        // Map command to model
        mapper.MapModel(pathogen, command);

        // Save changes to database
        await context.SaveChangesAsync(cancellationToken);

        return Respira.ServiceDefaults.Contracts.Results.Result.Success(Status.Updated);
    }
}
