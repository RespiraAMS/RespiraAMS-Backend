using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Antibiograms.DeleteAntibiogram;

public class DeleteAntibiogramHandler(IDbContext context, ILogger<DeleteAntibiogramHandler> logger)
    : ICommandHandler<DeleteAntibiogramCommand>
{
    public async Task HandleAsync(DeleteAntibiogramCommand command, CancellationToken cancellationToken = default)
    {
        // Get entity by ID
        var antibiogram = await context.Antibiograms
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (antibiogram is null)
        {
            logger.LogDebug("Antibiogram not found: {Id}", command.Id);
            throw new NotFoundException(nameof(Antibiogram), command.Id);
        }

        // Delete antibiogram
        antibiogram.IsDeleted = true;
        antibiogram.DeletedAt = DateTimeOffset.UtcNow;

        // Save changes to database
        if (await context.SaveChangesAsync(cancellationToken) <= 0)
        {
            logger.LogError("Failed to delete antibiogram");
            throw new ServerException();
        }
    }
}