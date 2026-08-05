using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Pathogens.UpdatePathogen;

public class UpdatePathogenHandler(
    IDbContext context,
    IUpdateMapper<Pathogen, UpdatePathogenCommand> mapper,
    ILogger<UpdatePathogenHandler> logger)
    : ICommandHandler<UpdatePathogenCommand>
{
    public async Task HandleAsync(UpdatePathogenCommand command, CancellationToken cancellationToken = default)
    {
        // Get pathogen by ID
        var pathogen = await context.Pathogens
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (pathogen is null)
        {
            logger.LogWarning("Pathogen ID not found");
            throw new NotFoundException(nameof(Pathogen), command.Id);
        }

        // Map command to model
        mapper.MapModel(pathogen, command);

        // Save changes to database
        if (await context.SaveChangesAsync(cancellationToken) <= 0)
        {
            logger.LogError("Failed to update pathogen");
            throw new ServerException();
        }
    }
}