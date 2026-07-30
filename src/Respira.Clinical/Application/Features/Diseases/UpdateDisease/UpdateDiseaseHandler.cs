using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Diseases.UpdateDisease;

public class UpdateDiseaseHandler(
    IDbContext context,
    IUpdateMapper<Disease, UpdateDiseaseCommand> mapper,
    ILogger<UpdateDiseaseHandler> logger)
    : ICommandHandler<UpdateDiseaseCommand>
{
    public async Task HandleAsync(UpdateDiseaseCommand command, CancellationToken cancellationToken = default)
    {
        // Get entity by ID
        var disease = await context.Diseases
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (disease is null)
        {
            logger.LogDebug("Disease not found: {Id}", command.Id);
            throw new NotFoundException(nameof(Disease), command.Id);
        }

        // Map command to model
        mapper.MapModel(disease, command);

        // Save changes to database
        if (await context.SaveChangesAsync(cancellationToken) <= 0)
        {
            logger.LogError("Failed to update disease to database: {Id}", command.Id);
            throw new ServerException();
        }
    }
}