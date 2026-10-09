using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Criteria.CreateCriterion
{
    public class CreateCriterionHandler(
        IDbContext context,
        ICreateMapper<CreateCriterionCommand, Criterion> mapper,
        ILogger<CreateCriterionHandler> logger)
        : ICommandHandler<CreateCriterionCommand, Result<CreateCriterionResult>>
    {
        public async Task<Result<CreateCriterionResult>> HandleAsync(CreateCriterionCommand command, CancellationToken cancellationToken = default)
        {
            // Get the list of clinical variable to form formula
            var variables = await context.ClinicalVariables.ToListAsync(cancellationToken);

            // Create criterion
            var mapResult = mapper.ToModel(command, variables);
            if (mapResult.IsFailure())
            {
                logger.LogDebug("Fail to map criterion: {error}", mapResult.Error!);
                return Result<CreateCriterionResult>.Failure(mapResult.Error!);
            }
            var criterion = mapResult.Data!;

            // We don't need to check for unique on criterion, because we can create
            // the same criterion that can be reference in multiple places. It's kind of
            // incorrect in term of database theory, but it make the system less vulnerable
            // when user update carelessly. For example, if 3 metrics all reference the same
            // criterion, and only 1 metrics need updated, the normal flow would be to cut off
            // the criterion from the updated metrics, then create a new criterion to avoid
            // modify other metrics. But since such flow is kind of intuitive, the chance that
            // user make an incorrect move is likely

            // Save changes to database
            await context.Criteria.AddAsync(criterion, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return Result<CreateCriterionResult>.Success(ApplicationStatus.Success, new CreateCriterionResult(criterion.Id));
        }
    }
}
