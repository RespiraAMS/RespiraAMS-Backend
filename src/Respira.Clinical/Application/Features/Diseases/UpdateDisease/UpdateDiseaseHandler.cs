using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Diseases.UpdateDisease;

public class UpdateDiseaseHandler(
    IDbContext context,
    IUpdateMapper<Disease, UpdateDiseaseCommand> mapper,
    ILogger<UpdateDiseaseHandler> logger)
    : ICommandHandler<UpdateDiseaseCommand, Respira.ServiceDefaults.Contracts.Results.Result>
{
    public async Task<Respira.ServiceDefaults.Contracts.Results.Result> HandleAsync(UpdateDiseaseCommand command, CancellationToken cancellationToken = default)
    {
        // Get entity by ID
        var disease = await context.Diseases
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (disease is null)
        {
            logger.LogDebug("Disease not found: {Id}", command.Id);
            return Respira.ServiceDefaults.Contracts.Results.Result.Failure(new Error(Status.BadRequest, "Disease ID not found"));
            // throw new NotFoundException(nameof(Disease), command.Id);
        }

        // Map command to model
        mapper.MapModel(disease, command);

        // Save changes to database
        await context.SaveChangesAsync(cancellationToken);
        return Respira.ServiceDefaults.Contracts.Results.Result.Success(Status.Updated);
    }
}
