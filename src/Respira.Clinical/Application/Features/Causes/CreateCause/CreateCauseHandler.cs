using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Causes.CreateCause;

public class CreateCauseHandler(
    IDbContext context,
    ICreateMapper<Cause, CreateCauseCommand> mapper,
    ILogger<CreateCauseHandler> logger)
    : ICommandHandler<CreateCauseCommand, Result<CreateCauseResult>>
{
    public async Task<Result<CreateCauseResult>> HandleAsync(CreateCauseCommand command,
        CancellationToken cancellationToken = default)
    {
        // Check if disease exists
        var disease = await context.Diseases
            .FirstOrDefaultAsync(x => x.Id == command.DiseaseId, cancellationToken);
        if (disease is null)
        {
            logger.LogDebug("Disease ID not found: {Id}", command.DiseaseId);
            return Result<CreateCauseResult>.Failure(new Error(Status.BadRequest, "Disease ID not exists"));
        }

        // Check if pathogen exists
        var pathogen = await context.Pathogens
            .FirstOrDefaultAsync(x => x.Id == command.PathogenId, cancellationToken);
        if (pathogen is null)
        {
            logger.LogDebug("Pathogen ID not found: {Id}", command.PathogenId);
            return Result<CreateCauseResult>.Failure(new Error(Status.BadRequest, "Pathogen ID not exists"));
        }

        // Check if this cause (disease, pathogen, severity, treatment site) exists.
        // Since we use soft delete, this should be checked on application level, not UNIQUE index
        // on db
        var causeDb = await context.Causes
            .Where(x =>
                x.DiseaseId == command.DiseaseId &&
                x.PathogenId == command.PathogenId &&
                x.Severity == command.Severity &&
                x.TreatmentSite == command.TreatmentSite)
            .FirstOrDefaultAsync(cancellationToken);
        if (causeDb is not null)
        {
            logger.LogDebug("Disease's cause duplicate: {cause}", command);
            return Result<CreateCauseResult>.Failure(new Error(Status.BadRequest, "Disease's cause duplicate"));
        }

        // Map command to model
        var cause = mapper.ToModel(command);

        // Save changes to database
        await context.Causes.AddAsync(cause, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result<CreateCauseResult>.Success(Status.Created, new CreateCauseResult(cause.Id));
    }
}
