using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.AntibioticGroups.CreateAntibioticGroup;

public class CreateAntibioticGroupHandler(
    IDbContext context,
    ICreateMapper<AntibioticGroup, CreateAntibioticGroupCommand> mapper,
    ILogger<CreateAntibioticGroupHandler> logger)
    : ICommandHandler<CreateAntibioticGroupCommand, CreateAntibioticGroupResult>
{
    public async Task<CreateAntibioticGroupResult> HandleAsync(CreateAntibioticGroupCommand command,
        CancellationToken cancellationToken = default)
    {
        // Check if parent ID exists in database if provided
        if (command.ParentId is not null)
        {
            var parent = await context.AntibioticGroups
                .FirstOrDefaultAsync(x => x.Id == command.ParentId, cancellationToken);
            if (parent is null)
            {
                logger.LogDebug("Parent ID not found for antibiotic group: {Id}", command.ParentId);
                throw new BadRequestException("Parent ID not exists");
            }            
        }
        
        // Map command to model
        var group = mapper.ToModel(command);

        // Save antibiotic group to database
        await context.AntibioticGroups.AddAsync(group, cancellationToken);
        if (await context.SaveChangesAsync(cancellationToken) <= 0)
        {
            logger.LogError("Failed to save antibiotic group");
            throw new ServerException();
        }

        return new CreateAntibioticGroupResult(group.Id);
    }
}