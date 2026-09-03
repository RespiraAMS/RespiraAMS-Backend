using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.ResistanceRiskFactors.UpdateResistanceRiskFactor;

public class UpdateResistanceRiskFactorHandler(
    IDbContext context,
    IUpdateMapper<ResistanceRiskFactor, UpdateResistanceRiskFactorCommand> mapper,
    ILogger<UpdateResistanceRiskFactorCommand> logger)
    : ICommandHandler<UpdateResistanceRiskFactorCommand, Result>
{
    public async Task<Result> HandleAsync(UpdateResistanceRiskFactorCommand command,
        CancellationToken cancellationToken = default)
    {
        // Check if pathogen exists
        var pathogen = await context.Pathogens
            .FirstOrDefaultAsync(x => x.Id == command.PathogenId, cancellationToken);
        if (pathogen is null)
        {
            logger.LogDebug("Pathogen ID not found: {Id}", command.PathogenId);
            return Result.Failure(new Error(Status.BadRequest, "Pathogen ID not exists"));
        }

        // Get entity by ID
        var factor = await context.ResistanceRiskFactors
            .Include(x => x.Criterion)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (factor is null)
        {
            logger.LogDebug("Resistance risk factor ID not found: {Id}", command.Id);
            return Result.Failure(new Error(Status.BadRequest, "Resistance risk factor ID not found"));
        }

        // Map from command to query
        mapper.MapModel(factor, command);

        // Save changes to database
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(Status.Updated);
    }
}
