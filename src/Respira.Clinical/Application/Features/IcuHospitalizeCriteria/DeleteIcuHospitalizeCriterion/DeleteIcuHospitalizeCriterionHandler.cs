using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.IcuHospitalizeCriteria.DeleteIcuHospitalizeCriterion;

public class DeleteIcuHospitalizeCriterionHandler(
    IDbContext context,
    ILogger<DeleteIcuHospitalizeCriterionHandler> logger)
    : ICommandHandler<DeleteIcuHospitalizeCriterionCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteIcuHospitalizeCriterionCommand command, CancellationToken cancellationToken = default)
    {
        // Get entity by ID
        var icu = await context.IcuHospitalizeCriteria
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (icu is null)
        {
            logger.LogDebug("ICU hospitalize criterion not found: {Id}", command.Id);
            return Result.Failure(new Error(Status.BadRequest, "ICU hospitalize criterion not found"));
        }

        // Delete ICU hospitalize criterion
        icu.IsDeleted = true;
        icu.DeletedAt = DateTimeOffset.UtcNow;

        // Save changes to database
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(Status.Deleted);
    }
}
