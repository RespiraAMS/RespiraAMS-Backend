using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Antibiotics.UpdateAntibioticSpectrum;

public class UpdateAntibioticSpectrumHandler(IDbContext context, ILogger<UpdateAntibioticSpectrumHandler> logger)
    : ICommandHandler<UpdateAntibioticSpectrumCommand, Respira.ServiceDefaults.Contracts.Results.Result>
{
    public async Task<Respira.ServiceDefaults.Contracts.Results.Result> HandleAsync(UpdateAntibioticSpectrumCommand command, CancellationToken cancellationToken = default)
    {
        // Get entity by ID
        var antibiotic = await context.Antibiotics
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (antibiotic is null)
        {
            logger.LogDebug("Antibiotic not found: {Id}", command.Id);
            return Respira.ServiceDefaults.Contracts.Results.Result.Failure(new Error(Status.BadRequest, "Antibiotic not found"));
            // throw new NotFoundException(nameof(Antibiotic), command.Id);
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
            return Respira.ServiceDefaults.Contracts.Results.Result.Failure(new Error(Status.BadRequest, "Not all pathogen IDs provided exists in database"));
            // throw new BadRequestException("Not all pathogen IDs provided exists in database");
        }

        // Update in database
        context.UpdateRelations(antibiotic.AntibioticSpectra, command.PathogenIds);
        await context.SaveChangesAsync(cancellationToken);
        return Respira.ServiceDefaults.Contracts.Results.Result.Success(Status.Updated);
    }
}
