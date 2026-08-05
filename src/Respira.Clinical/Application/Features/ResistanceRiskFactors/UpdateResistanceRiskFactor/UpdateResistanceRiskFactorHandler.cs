using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.ResistanceRiskFactors.UpdateResistanceRiskFactor;

public class UpdateResistanceRiskFactorHandler(
    IDbContext context,
    IUpdateMapper<ResistanceRiskFactor, UpdateResistanceRiskFactorCommand> mapper,
    ILogger<UpdateResistanceRiskFactorCommand> logger)
    : ICommandHandler<UpdateResistanceRiskFactorCommand>
{
    public async Task HandleAsync(UpdateResistanceRiskFactorCommand command,
        CancellationToken cancellationToken = default)
    {
        // Check if pathogen exists
        var pathogen = await context.Pathogens
            .FirstOrDefaultAsync(x => x.Id == command.PathogenId, cancellationToken);
        if (pathogen is null)
        {
            logger.LogDebug("Pathogen ID not found: {Id}", command.PathogenId);
            throw new BadRequestException("Pathogen ID not exists");
        }

        // Get entity by ID
        var factor = await context.ResistanceRiskFactors
            .Include(x => x.Criterion)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (factor is null)
        {
            logger.LogDebug("Resistance risk factor ID not found: {Id}", command.Id);
            throw new NotFoundException(nameof(ResistanceRiskFactor), command.Id);
        }

        // Map from command to query
        mapper.MapModel(factor, command);

        // Save changes to database
        if (await context.SaveChangesAsync(cancellationToken) <= 0)
        {
            logger.LogError("Failed to save resistance risk factor");
            throw new ServerException();
        }
    }
}