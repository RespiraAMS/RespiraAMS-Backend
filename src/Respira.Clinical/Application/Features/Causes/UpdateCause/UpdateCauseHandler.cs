using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Causes.UpdateCause;

public class UpdateCauseHandler(
    IDbContext context,
    IUpdateMapper<Cause, UpdateCauseCommand> mapper,
    ILogger<UpdateCauseHandler> logger)
    : ICommandHandler<UpdateCauseCommand, Respira.ServiceDefaults.Contracts.Results.Result>
{
    public async Task<Respira.ServiceDefaults.Contracts.Results.Result> HandleAsync(UpdateCauseCommand command, CancellationToken cancellationToken = default)
    {
        // Get entity by ID
        var cause = await context.Causes
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (cause is null)
        {
            logger.LogDebug("Disease's cause ID not found: {Id}", command.Id);
            return Respira.ServiceDefaults.Contracts.Results.Result.Failure(new Error(Status.BadRequest, "Disease's cause ID not found"));
            // throw new NotFoundException(nameof(Cause), command.Id);
        }

        // Check if the new severity/treatment site will cause a duplicate problem
        var hasDuplicate = await context.Causes
            .Where(x =>
                x.Id != command.Id &&
                x.DiseaseId == cause.DiseaseId &&
                x.PathogenId == cause.PathogenId &&
                x.Severity == command.Severity &&
                x.TreatmentSite == command.TreatmentSite)
            .AnyAsync(cancellationToken);
        if (hasDuplicate)
        {
            logger.LogDebug("New severity and treatment site cause duplicate data: {command}", command);
            return Respira.ServiceDefaults.Contracts.Results.Result.Failure(new Error(Status.BadRequest, "New severity and treatment site cause duplicate data"));
            // throw new BadRequestException("The disease cause with this severity and treatment site is already exists");
        }

        // Map command to model
        mapper.MapModel(cause, command);

        // Save changes to database
        await context.SaveChangesAsync(cancellationToken);
        return Respira.ServiceDefaults.Contracts.Results.Result.Success(Status.Updated);
    }
}
