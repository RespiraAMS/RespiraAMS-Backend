using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Antibiotics.DeleteAntibiotic;

public class DeleteAntibioticHandler(IDbContext context, ILogger<DeleteAntibioticHandler> logger)
    : ICommandHandler<DeleteAntibioticCommand>
{
    public async Task HandleAsync(DeleteAntibioticCommand command, CancellationToken cancellationToken = default)
    {
        // Get entity by ID
        var antibiotic = await context.Antibiotics
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (antibiotic is null)
        {
            logger.LogDebug("Antibiotic not found: {Id}", command.Id);
            throw new NotFoundException(nameof(Antibiotic), command.Id);
        }

        // Delete antibiotic
        await context.ExecuteInTransactionAsync(async () =>
        {
            // Soft delete antibiotic
            antibiotic.IsDeleted = true;
            antibiotic.DeletedAt = DateTimeOffset.UtcNow;

            // Cascade delete all dosage belong to this antibiotic
            var count = await context.Dosages.ExecuteUpdateAsync(x => x
                    .SetProperty(d => d.IsDeleted, true)
                    .SetProperty(d => d.DeletedAt, DateTimeOffset.UtcNow), cancellationToken);

            logger.LogDebug("Cascade delete {count} antibiotics when delete antibiotic {Id}", count, command.Id);
        }, cancellationToken);
    }
}