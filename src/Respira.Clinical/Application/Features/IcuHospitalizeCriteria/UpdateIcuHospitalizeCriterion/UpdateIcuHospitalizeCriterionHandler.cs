using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.IcuHospitalizeCriteria.UpdateIcuHospitalizeCriterion;

public class UpdateIcuHospitalizeCriterionHandler(
    IDbContext context,
    IUpdateMapper<IcuHospitalizeCriterion, UpdateIcuHospitalizeCriterionCommand> mapper,
    ILogger<UpdateIcuHospitalizeCriterionHandler> logger)
    : ICommandHandler<UpdateIcuHospitalizeCriterionCommand, Result>
{
    public async Task<Result> HandleAsync(UpdateIcuHospitalizeCriterionCommand command, CancellationToken cancellationToken = default)
    {
        // Get entity by ID
        var icu = await context.IcuHospitalizeCriteria
            .Include(x => x.Criterion)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (icu is null)
        {
            logger.LogDebug("ICU hospitalize criterion not found: {Id}", command.Id);
            return Result.Failure(new Error(Status.BadRequest, "ICU hospitalize criterion not found"));
        }

        // Map from command to model
        mapper.MapModel(icu, command);

        // Save changes to database
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(Status.Updated);
    }
}
