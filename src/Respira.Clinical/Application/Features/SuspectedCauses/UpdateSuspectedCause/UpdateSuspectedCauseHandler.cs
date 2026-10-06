using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.SuspectedCauses.UpdateSuspectedCause
{
    public class UpdateSuspectedCauseHandler(
        IDbContext context,
        IUpdateMapper<SuspectedCause, UpdateSuspectedCauseCommand> mapper,
        ILogger<UpdateSuspectedCauseHandler> logger)
        : ICommandHandler<UpdateSuspectedCauseCommand, Result>
    {
        public async Task<Result> HandleAsync(UpdateSuspectedCauseCommand command, CancellationToken cancellationToken = default)
        {
            // Get entity by ID
            var cause = await context.SuspectedCauses
                .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
            if (cause is null)
            {
                logger.LogDebug("Disease's cause ID not found: {Id}", command.Id);
                return Result.Failure(new Error(ApplicationStatus.BadRequest, "Disease's cause ID not found"));
            }

            // Check if pathogen exists
            var pathogen = await context.Pathogens.FirstOrDefaultAsync(x => x.Id == command.PathogenId, cancellationToken);
            if (pathogen is null)
            {
                logger.LogDebug("Pathogen ID not found: {Id}", command.PathogenId);
                return Result.Failure(new Error(ApplicationStatus.BadRequest, "Pathogen ID not exists"));
            }

            // Check if the new severity/treatment site will cause a duplicate problem
            var hasDuplicate = await context.SuspectedCauses
                .Where(x =>
                    x.Id != command.Id &&
                    x.PathogenId == cause.PathogenId &&
                    x.Severity == command.Severity &&
                    x.TreatmentSite == command.TreatmentSite)
                .AnyAsync(cancellationToken);
            if (hasDuplicate)
            {
                logger.LogDebug("New severity and treatment site cause duplicate data: {command}", command);
                return Result.Failure(new Error(ApplicationStatus.BadRequest, "New severity and treatment site cause duplicate data"));
            }

            // Map command to model
            var mapResult = mapper.MapModel(cause, command);
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
