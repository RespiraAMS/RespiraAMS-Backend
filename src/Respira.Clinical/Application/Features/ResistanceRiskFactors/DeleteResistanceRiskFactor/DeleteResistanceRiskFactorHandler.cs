using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.ResistanceRiskFactors.DeleteResistanceRiskFactor;

public class DeleteResistanceRiskFactorHandler(IDbContext context, ILogger<DeleteResistanceRiskFactorHandler> logger)
    : ICommandHandler<DeleteResistanceRiskFactorCommand>
{
    public async Task HandleAsync(DeleteResistanceRiskFactorCommand command,
        CancellationToken cancellationToken = default)
    {
        // Get entity by ID
        var factor = await context.ResistanceRiskFactors
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (factor is null)
        {
            logger.LogDebug("Resistance risk factor ID not found: {Id}", command.Id);
            throw new NotFoundException(nameof(ResistanceRiskFactor), command.Id);
        }

        // Delete factor
        factor.IsDeleted = true;
        factor.DeletedAt = DateTimeOffset.UtcNow;

        // Save changes to database
        if (await context.SaveChangesAsync(cancellationToken) <= 0)
        {
            logger.LogError("Failed to delete resistance risk factor");
            throw new ServerException();
        }
    }
}