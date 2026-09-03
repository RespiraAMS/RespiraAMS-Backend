using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.ResistanceRiskFactors.DeleteResistanceRiskFactor;

public class DeleteResistanceRiskFactorHandler(IDbContext context, ILogger<DeleteResistanceRiskFactorHandler> logger)
    : ICommandHandler<DeleteResistanceRiskFactorCommand, Respira.ServiceDefaults.Contracts.Results.Result>
{
    public async Task<Respira.ServiceDefaults.Contracts.Results.Result> HandleAsync(DeleteResistanceRiskFactorCommand command,
        CancellationToken cancellationToken = default)
    {
        // Get entity by ID
        var factor = await context.ResistanceRiskFactors
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (factor is null)
        {
            logger.LogDebug("Resistance risk factor ID not found: {Id}", command.Id);
            return Respira.ServiceDefaults.Contracts.Results.Result.Failure(new Error(Status.BadRequest, "Resistance risk factor ID not found"));
            // throw new NotFoundException(nameof(ResistanceRiskFactor), command.Id);
        }

        // Delete factor
        factor.IsDeleted = true;
        factor.DeletedAt = DateTimeOffset.UtcNow;

        // Save changes to database
        await context.SaveChangesAsync(cancellationToken);
        return Respira.ServiceDefaults.Contracts.Results.Result.Success(Status.Deleted);
    }
}
