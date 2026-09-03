using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.AntibioticGroups.CreateAntibioticGroup;

public class CreateAntibioticGroupHandler(
    IDbContext context,
    ICreateMapper<AntibioticGroup, CreateAntibioticGroupCommand> mapper,
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
                return Result<CreateAntibioticGroupResult>.Failure(new Error(Status.BadRequest, "Antibiotic group parent ID not found"));
            }
        }

        // Map command to model
        var group = mapper.ToModel(command);

        // Save antibiotic group to database
        await context.AntibioticGroups.AddAsync(group, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return Result<CreateAntibioticGroupResult>.Success(Status.Created, new CreateAntibioticGroupResult(group.Id));
    }
}
