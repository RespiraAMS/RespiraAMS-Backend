using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Pathogens.DeletePathogen;

public class DeletePathogenHandler(IDbContext context, ILogger<DeletePathogenHandler> logger)
    : ICommandHandler<DeletePathogenCommand>
{
    public async Task HandleAsync(DeletePathogenCommand command, CancellationToken cancellationToken = default)
    {
        // Get pathogen by ID
        var pathogen = await context.Pathogens
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (pathogen is null)
        {
            logger.LogWarning("Pathogen ID not found");
            throw new NotFoundException(nameof(Pathogen), command.Id);
        }

        // Delete cascade in transaction
        await context.ExecuteInTransactionAsync(async () =>
        {
            // Delete pathogen
            pathogen.IsDeleted = true;
            pathogen.DeletedAt = DateTimeOffset.UtcNow;

            // Cascade delete: ResistanceRiskFactor, Cause, EmpiricTreatmentProtocol
            var protocolCount = await context.EmpiricTreatmentProtocols
                .Where(x => x.SpecialInfectionId == command.Id)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(p => p.IsDeleted, true)
                    .SetProperty(p => p.DeletedAt, DateTimeOffset.UtcNow), cancellationToken);
            var riskCount = await context.ResistanceRiskFactors
                .Where(x => x.PathogenId == pathogen.Id)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(r => r.IsDeleted, true)
                    .SetProperty(r => r.DeletedAt, DateTimeOffset.UtcNow), cancellationToken);
            var diseasePathogenCount = await context.Causes
                .Where(x => x.PathogenId == pathogen.Id)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(dp => dp.IsDeleted, true)
                    .SetProperty(dp => dp.DeletedAt, DateTimeOffset.UtcNow), cancellationToken);

            // Log result
            logger.LogInformation("Cascade delete pathogen: {result}", new
            {
                TreatmentProtocolCount = protocolCount,
                ResistanceRiskFactorCount = riskCount,
                DiseasePathogenCount = diseasePathogenCount
            });
        }, cancellationToken);
    }
}