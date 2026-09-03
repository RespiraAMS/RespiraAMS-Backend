using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Diseases.UpdateDisease;

public class UpdateDiseaseHandler(
    IDbContext context,
    IUpdateMapper<Disease, UpdateDiseaseCommand> mapper,
    ILogger<UpdateDiseaseHandler> logger)
    : ICommandHandler<UpdateDiseaseCommand, Result>
{
    public async Task<Result> HandleAsync(UpdateDiseaseCommand command, CancellationToken cancellationToken = default)
    {
        // Get entity by ID
        var disease = await context.Diseases
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (disease is null)
        {
            logger.LogDebug("Disease not found: {Id}", command.Id);
            return Result.Failure(new Error(Status.BadRequest, "Disease ID not found"));
        }

        // Map command to model
        mapper.MapModel(disease, command);

        // Save changes to database
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(Status.Updated);
    }
}
