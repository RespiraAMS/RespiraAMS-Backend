using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.SuspectedCauses.DeleteSuspectedCause
{
    public class DeleteSuspectedCauseHandler(IDbContext context, ILogger<DeleteSuspectedCauseHandler> logger)
        : ICommandHandler<DeleteSuspectedCauseCommand, Result>
    {
        public async Task<Result> HandleAsync(DeleteSuspectedCauseCommand command, CancellationToken cancellationToken = default)
        {
            // Get entity by ID
            var cause = await context.SuspectedCauses.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
            if (cause is null)
            {
                logger.LogDebug("Disease cause with this ID not found: {Id}", command.Id);
                return Result.Failure(new Error(ApplicationStatus.BadRequest, "Disease cause with this ID not found"));
            }

            // Delete cause
            cause.IsDeleted = true;
            cause.DeletedAt = DateTimeOffset.UtcNow;

            // Save changes to database
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(ApplicationStatus.Deleted);
        }
    }
}
