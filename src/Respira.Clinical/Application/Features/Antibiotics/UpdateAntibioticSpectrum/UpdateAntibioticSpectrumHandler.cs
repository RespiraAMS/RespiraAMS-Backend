using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Antibiotics.UpdateAntibioticSpectrum;

public class UpdateAntibioticSpectrumHandler(IDbContext context, ILogger<UpdateAntibioticSpectrumHandler> logger)
    : ICommandHandler<UpdateAntibioticSpectrumCommand>
{
    public async Task HandleAsync(UpdateAntibioticSpectrumCommand command,
        CancellationToken cancellationToken = default)
    {
        // Get entity by ID
        var antibiotic = await context.Antibiotics
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (antibiotic is null)
        {
            logger.LogDebug("Antibiotic not found: {Id}", command.Id);
            throw new NotFoundException(nameof(Antibiotic), command.Id);
        }

        // Check if pathogen IDs exist
        var pathogenCount = await context.Pathogens
            .CountAsync(x => command.PathogenIds.Contains(x.Id), cancellationToken);
        if (pathogenCount != command.PathogenIds.Count)
        {
            logger.LogDebug("Not all pathogen IDs provided exists in database: {result}", new
            {
                PathogenDbCount = pathogenCount,
                PathogenProvided = command.PathogenIds.Count
            });
            throw new BadRequestException("Not all pathogen IDs provided exists in database");
        }

        // Update in database
        context.UpdateRelations(antibiotic.AntibioticSpectra, command.PathogenIds);
        if (await context.SaveChangesAsync(cancellationToken) <= 0)
        {
            logger.LogError("Failed to save antibiotic");
            throw new ServerException();
        }
    }
}