using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Causes.DeleteCause;

public class DeleteCauseHandler(IDbContext context, ILogger<DeleteCauseHandler> logger)
    : ICommandHandler<DeleteCauseCommand>
{
    public async Task HandleAsync(DeleteCauseCommand command, CancellationToken cancellationToken = default)
    {
        // Get entity by ID
        var cause = await context.Causes.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (cause is null)
        {
            logger.LogDebug("Disease cause with this ID not found: {Id}", command.Id);
            throw new NotFoundException(nameof(Cause), command.Id);
        }

        // Delete cause
        cause.IsDeleted = true;
        cause.DeletedAt = DateTimeOffset.UtcNow;

        // Save changes to database
        if (await context.SaveChangesAsync(cancellationToken) <= 0)
        {
            logger.LogError("Failed to delete disease's cause");
            throw new ServerException();
        }
    }
}