using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.AntibioticGroups.UpdateAntibioticGroup
{
    public class UpdateAntibioticGroupHandler(
        IDbContext context,
        IUpdateMapper<AntibioticGroup, UpdateAntibioticGroupCommand> mapper,
        ILogger<UpdateAntibioticGroupHandler> logger) : ICommandHandler<UpdateAntibioticGroupCommand, Result>
    {
        public async Task<Result> HandleAsync(UpdateAntibioticGroupCommand command, CancellationToken cancellationToken = default)
        {
            // Check if parent ID exists if provided
            if (command.ParentId is not null)
            {
                var parent = await context.AntibioticGroups
                    .FirstOrDefaultAsync(x => x.Id == command.ParentId, cancellationToken);
                if (parent is null)
                {
                    logger.LogDebug("Parent ID not found for antibiotic group: {Id}", command.ParentId);
                    return Result.Failure(new Error(ApplicationStatus.BadRequest, "Antibiotic group parent ID not found"));
                }
            }

            // Get entity from database
            var group = await context.AntibioticGroups
                .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
            if (group is null)
            {
                logger.LogDebug("Antibiotic group not found: {Id}", command.Id);
                return Result.Failure(new Error(ApplicationStatus.BadRequest, "Antibiotic group not found"));
            }

            // Map from command to model
            var mapResult = mapper.MapModel(group, command);
            if (mapResult.IsFailure())
            {
                logger.LogDebug("Failed to map command to model: {Error}", mapResult.Error);
                return Result.Failure(mapResult.Error!);
            }

            // Save changes to database
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(ApplicationStatus.Updated);
        }
    }
}
