using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.IcuHospitalizeCriteria.DeleteIcuHospitalizeCriterion;

public class DeleteIcuHospitalizeCriterionHandler(
    IDbContext context,
    ILogger<DeleteIcuHospitalizeCriterionHandler> logger)
    : ICommandHandler<DeleteIcuHospitalizeCriterionCommand>
{
    public async Task HandleAsync(DeleteIcuHospitalizeCriterionCommand command,
        CancellationToken cancellationToken = default)
    {
        // Get entity by ID
        var icu = await context.IcuHospitalizeCriteria
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (icu is null)
        {
            logger.LogDebug("ICU hospitalize criterion not found: {Id}", command.Id);
            throw new NotFoundException(nameof(IcuHospitalizeCriteria), command.Id);
        }

        // Delete ICU hospitalize criterion
        icu.IsDeleted = true;
        icu.DeletedAt = DateTimeOffset.UtcNow;

        // Save changes to database
        if (await context.SaveChangesAsync(cancellationToken) <= 0)
        {
            logger.LogError("Failed to delete disease's ICU hospitalize criterion");
            throw new ServerException();
        }
    }
}