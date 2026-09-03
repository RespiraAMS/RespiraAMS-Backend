using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Causes.DeleteCause;

public class DeleteCauseHandler(IDbContext context, ILogger<DeleteCauseHandler> logger)
    : ICommandHandler<DeleteCauseCommand, Respira.ServiceDefaults.Contracts.Results.Result>
{
    public async Task<Respira.ServiceDefaults.Contracts.Results.Result> HandleAsync(DeleteCauseCommand command, CancellationToken cancellationToken = default)
    {
        // Get entity by ID
        var cause = await context.Causes.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (cause is null)
        {
            logger.LogDebug("Disease cause with this ID not found: {Id}", command.Id);
            return Respira.ServiceDefaults.Contracts.Results.Result.Failure(new Error(Status.BadRequest, "Disease cause with this ID not found"));
            // throw new NotFoundException(nameof(Cause), command.Id);
        }

        // Delete cause
        cause.IsDeleted = true;
        cause.DeletedAt = DateTimeOffset.UtcNow;

        // Save changes to database
        await context.SaveChangesAsync(cancellationToken);
        return Respira.ServiceDefaults.Contracts.Results.Result.Success(Status.Deleted);
    }
}
