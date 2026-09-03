using Application.Contracts.Data;
using Application.Features.Shared.ManageCriterion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.EmpiricTreatmentProtocols.AddNewCriteria;

public class AddNewCriteriaHandler(
    IDbContext context,
    ICreateMapper<Criterion, CreateCriterionCommand> mapper,
    ILogger<AddNewCriteriaHandler> logger)
    : ICommandHandler<AddNewCriteriaCommand, Respira.ServiceDefaults.Contracts.Results.Result>
{
    public async Task<Respira.ServiceDefaults.Contracts.Results.Result> HandleAsync(AddNewCriteriaCommand command, CancellationToken cancellationToken = default)
    {
        // Get treatment protocol by ID
        var protocol = await context.EmpiricTreatmentProtocols
            .Include(x => x.OtherCriteria)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (protocol is null)
        {
            logger.LogWarning("Empiric treatment protocol ID not found");
            return Respira.ServiceDefaults.Contracts.Results.Result.Failure(new Error(Status.BadRequest, "Empiric treatment protocol ID not found"));
            // throw new NotFoundException(nameof(EmpiricTreatmentProtocol), command.Id);
        }

        // Map request to models
        var criteria = command.Criteria.ConvertAll(mapper.ToModel);

        // Start transaction
        await context.ExecuteInTransactionAsync(async () =>
        {
            // Add batch
            await context.Criteria.AddRangeAsync(criteria, cancellationToken);

            // Add the created list of criteria into the treatment protocol list
            // Since criteria list is already tracked by EF Core, we don't need to use stub
            protocol.OtherCriteria.AddRange(criteria);
        }, cancellationToken);
        return Respira.ServiceDefaults.Contracts.Results.Result.Success(Status.Created);
    }
}
