using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Causes.DeleteCause;

public class DeleteCauseHandler(IDbContext context, ILogger<DeleteCauseHandler> logger)
    : ICommandHandler<DeleteCauseCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteCauseCommand command, CancellationToken cancellationToken = default)
    {
        // Get entity by ID
        var cause = await context.Causes.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (cause is null)
        {
            logger.LogDebug("Disease cause with this ID not found: {Id}", command.Id);
            return Result.Failure(new Error(Status.BadRequest, "Disease cause with this ID not found"));
        }

        // Delete cause
        cause.IsDeleted = true;
        cause.DeletedAt = DateTimeOffset.UtcNow;

        // Save changes to database
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(Status.Deleted);
    }
}
