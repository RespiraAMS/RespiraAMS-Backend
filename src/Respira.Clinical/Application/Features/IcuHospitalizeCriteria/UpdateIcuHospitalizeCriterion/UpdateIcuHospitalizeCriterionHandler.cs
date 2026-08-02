using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.IcuHospitalizeCriteria.UpdateIcuHospitalizeCriterion;

public class UpdateIcuHospitalizeCriterionHandler(
    IDbContext context,
    IUpdateMapper<IcuHospitalizeCriterion, UpdateIcuHospitalizeCriterionCommand> mapper,
    ILogger<UpdateIcuHospitalizeCriterionHandler> logger)
    : ICommandHandler<UpdateIcuHospitalizeCriterionCommand>
{
    public async Task HandleAsync(UpdateIcuHospitalizeCriterionCommand command,
        CancellationToken cancellationToken = default)
    {
        // Get entity by ID
        var icu = await context.IcuHospitalizeCriteria
            .Include(x => x.Criterion)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (icu is null)
        {
            logger.LogDebug("ICU hospitalize criterion not found: {Id}", command.Id);
            throw new NotFoundException(nameof(IcuHospitalizeCriteria), command.Id);
        }

        // Map from command to model
        mapper.MapModel(icu, command);

        // Save changes to database
        if (await context.SaveChangesAsync(cancellationToken) <= 0)
        {
            logger.LogError("Failed to save disease's ICU hospitalize criterion");
            throw new ServerException();
        }
    }
}