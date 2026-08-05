using Application.Contracts.Data;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Antibiotics.CreateAntibiotic;

public class CreateAntibioticHandler(
    IDbContext context,
    ICreateMapper<Antibiotic, CreateAntibioticCommand> mapper,
    ILogger<CreateAntibioticHandler> logger)
    : ICommandHandler<CreateAntibioticCommand, CreateAntibioticResult>
{
    public async Task<CreateAntibioticResult> HandleAsync(CreateAntibioticCommand command,
        CancellationToken cancellationToken = default)
    {
        // Check if antibiotic group exists
        var group = await context.AntibioticGroups
            .FirstOrDefaultAsync(x => x.Id == command.AntibioticGroupId, cancellationToken);
        if (group is null)
        {
            logger.LogDebug("Antibiotic group ID not found for antibiotic group: {Id}", command.AntibioticGroupId);
            throw new BadRequestException("Antibiotic group ID not exists");
        }

        // Map command to model
        var antibiotic = mapper.ToModel(command);

        // Save changes to database
        await context.Antibiotics.AddAsync(antibiotic, cancellationToken);
        if (await context.SaveChangesAsync(cancellationToken) <= 0)
        {
            logger.LogError("Failed to save antibiotic");
            throw new ServerException();
        }

        return new CreateAntibioticResult(antibiotic.Id);
    }
}