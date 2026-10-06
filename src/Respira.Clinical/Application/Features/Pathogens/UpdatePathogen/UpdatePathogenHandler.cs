using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Pathogens.UpdatePathogen
{
    public class UpdatePathogenHandler(
        IDbContext context,
        IUpdateMapper<Pathogen, UpdatePathogenCommand> mapper,
        ILogger<UpdatePathogenHandler> logger)
        : ICommandHandler<UpdatePathogenCommand, Result>
    {
        public async Task<Result> HandleAsync(UpdatePathogenCommand command, CancellationToken cancellationToken = default)
        {
            // Get pathogen by ID
            var pathogen = await context.Pathogens.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
            if (pathogen is null)
            {
                logger.LogWarning("Pathogen ID not found");
                return Result.Failure(new Error(ApplicationStatus.BadRequest, "Pathogen ID not found"));
            }

            // Map command to model
            var mapResult = mapper.MapModel(pathogen, command);
            if (mapResult.IsFailure())
            {
                logger.LogDebug("Failed to map command to model: {Error}", mapResult.Error);
                return Result.Failure(mapResult.Error!);
            }

            // Save changes to database
            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(ApplicationStatus.Updated);
        }
    }
}
