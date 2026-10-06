using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.AntibioticGroups.CreateAntibioticGroup
{
    public class CreateAntibioticGroupHandler(
        IDbContext context,
        ICreateMapper<CreateAntibioticGroupCommand, AntibioticGroup> mapper,
        ILogger<CreateAntibioticGroupHandler> logger)
        : ICommandHandler<CreateAntibioticGroupCommand, Result<CreateAntibioticGroupResult>>
    {
        public async Task<Result<CreateAntibioticGroupResult>> HandleAsync(CreateAntibioticGroupCommand command, CancellationToken cancellationToken = default)
        {
            // Check if parent ID exists in database if provided
            if (command.ParentId is not null)
            {
                var parent = await context.AntibioticGroups
                    .FirstOrDefaultAsync(x => x.Id == command.ParentId, cancellationToken);
                if (parent is null)
                {
                    logger.LogDebug("Parent ID not found for antibiotic group: {Id}", command.ParentId);
                    return Result<CreateAntibioticGroupResult>.Failure(new Error(ApplicationStatus.BadRequest, "Antibiotic group parent ID not found"));
                }
            }

            // Map command to model
            var mapResult = mapper.ToModel(command);
            if (mapResult.IsFailure())
            {
                logger.LogDebug("Failed to map command to model: {Error}", mapResult.Error);
                return Result<CreateAntibioticGroupResult>.Failure(mapResult.Error!);
            }
            var group = mapResult.Data!;

            // Save antibiotic group to database
            await context.AntibioticGroups.AddAsync(group, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result<CreateAntibioticGroupResult>.Success(ApplicationStatus.Created, new CreateAntibioticGroupResult(group.Id));
        }
    }
}
