using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Criteria.UpdateCriterion
{
    public class UpdateCriterionHandler(
        IDbContext context,
        IUpdateMapper<Criterion, UpdateCriterionCommand> mapper,
        ILogger<UpdateCriterionHandler> logger)
        : ICommandHandler<UpdateCriterionCommand, Result>
    {
        public async Task<Result> HandleAsync(UpdateCriterionCommand command, CancellationToken cancellationToken = default)
        {
            // Get the criterion to update
            var criterion = await context.Criteria.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
            if (criterion is null)
            {
                logger.LogDebug("Criterion with this ID not found: {Id}", command.Id);
                return Result.Failure(new Error(ApplicationStatus.BadRequest, "Criterion not found"));
            }

            // Get the list of variables for formula mapping
            var variables = await context.ClinicalVariables.ToListAsync(cancellationToken);

            // Map command to model
            var mapResult = mapper.MapModel(criterion, command, variables);
            if (mapResult.IsFailure())
            {
                logger.LogDebug("Failed to map criterion: {error}", mapResult.Error!);
                return Result.Failure(mapResult.Error!);
            }

            // Save changes to database
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(ApplicationStatus.Updated);
        }
    }
}
