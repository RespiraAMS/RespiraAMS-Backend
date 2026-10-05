using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.SuspectedCauses.CreateSuspectedCause
{
    public class CreateSuspectedCauseHandler(
        IDbContext context,
        ICreateMapper<SuspectedCause, CreateSuspectedCauseCommand> mapper,
        ILogger<CreateSuspectedCauseHandler> logger)
        : ICommandHandler<CreateSuspectedCauseCommand, Result<CreateSuspectedCauseResult>>
    {
        public async Task<Result<CreateSuspectedCauseResult>> HandleAsync(CreateSuspectedCauseCommand command, CancellationToken cancellationToken = default)
        {
            // Check if pathogen exists
            var pathogen = await context.Pathogens.FirstOrDefaultAsync(x => x.Id == command.PathogenId, cancellationToken);
            if (pathogen is null)
            {
                logger.LogDebug("Pathogen ID not found: {Id}", command.PathogenId);
                return Result<CreateSuspectedCauseResult>.Failure(new Error(ApplicationStatus.BadRequest, "Pathogen ID not exists"));
            }

            // Check if this cause (pathogen, severity, treatment site) exists.
            // Since we use soft delete, this should be checked on application level, not UNIQUE index
            // on db
            var causeDb = await context.SuspectedCauses
                .Where(x =>
                    x.PathogenId == command.PathogenId &&
                    x.Severity == command.Severity &&
                    x.TreatmentSite == command.TreatmentSite)
                .FirstOrDefaultAsync(cancellationToken);
            if (causeDb is not null)
            {
                logger.LogDebug("Disease's cause duplicate: {cause}", command);
                return Result<CreateSuspectedCauseResult>.Failure(new Error(ApplicationStatus.BadRequest, "Disease's cause duplicate"));
            }

            // Map command to model
            var cause = mapper.ToModel(command);

            // Save changes to database
            await context.SuspectedCauses.AddAsync(cause, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return Result<CreateSuspectedCauseResult>.Success(ApplicationStatus.Created, new CreateSuspectedCauseResult(cause.Id));
        }
    }
}
