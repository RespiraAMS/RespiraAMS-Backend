using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.ResistanceRiskFactors.DeleteResistanceRiskFactor;

public class DeleteResistanceRiskFactorHandler(IDbContext context, ILogger<DeleteResistanceRiskFactorHandler> logger)
    : ICommandHandler<DeleteResistanceRiskFactorCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteResistanceRiskFactorCommand command,
        CancellationToken cancellationToken = default)
    {
        // Get entity by ID
        var factor = await context.ResistanceRiskFactors
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (factor is null)
        {
            logger.LogDebug("Resistance risk factor ID not found: {Id}", command.Id);
            return Result.Failure(new Error(Status.BadRequest, "Resistance risk factor ID not found"));
        }

        // Delete factor
        factor.IsDeleted = true;
        factor.DeletedAt = DateTimeOffset.UtcNow;

        // Save changes to database
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(Status.Deleted);
    }
}
