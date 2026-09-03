using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Antibiograms.DeleteAntibiogram;

public class DeleteAntibiogramHandler(IDbContext context, ILogger<DeleteAntibiogramHandler> logger)
    : ICommandHandler<DeleteAntibiogramCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteAntibiogramCommand command, CancellationToken cancellationToken = default)
    {
        // Get entity by ID
        var antibiogram = await context.Antibiograms.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (antibiogram is null)
        {
            logger.LogDebug("Antibiogram not found: {Id}", command.Id);
            return Result.Failure(new Error(Status.BadRequest, "Antibiogram not found"));
        }

        // Delete antibiogram
        antibiogram.IsDeleted = true;
        antibiogram.DeletedAt = DateTimeOffset.UtcNow;

        // Save changes to database
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(Status.Deleted);
    }
}
