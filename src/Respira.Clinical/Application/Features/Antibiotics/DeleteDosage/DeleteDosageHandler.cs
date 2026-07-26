using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Antibiotics.DeleteDosage;

public class DeleteDosageHandler(IDbContext context, ILogger<DeleteDosageHandler> logger)
    : ICommandHandler<DeleteDosageCommand>
{
    public async Task HandleAsync(DeleteDosageCommand command, CancellationToken cancellationToken = default)
    {
        // Get entity by ID
        var dosage = await context.Dosages.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (dosage is null)
        {
            logger.LogDebug("Antibiotic dosage not found: {Id}", command.Id);
            throw new NotFoundException(nameof(Dosage), command.Id);
        }

        // Delete dosage
        dosage.IsDeleted = true;
        dosage.DeletedAt = DateTimeOffset.UtcNow;
        if (await context.SaveChangesAsync(cancellationToken) <= 0)
        {
            logger.LogError("Failed to delete antibiotic dosage");
            throw new ServerException();
        }
    }
}