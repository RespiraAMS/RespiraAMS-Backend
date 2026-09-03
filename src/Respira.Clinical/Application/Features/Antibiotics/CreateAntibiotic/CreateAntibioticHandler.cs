using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Antibiotics.CreateAntibiotic;

public class CreateAntibioticHandler(
    IDbContext context,
    ICreateMapper<Antibiotic, CreateAntibioticCommand> mapper,
    ILogger<CreateAntibioticHandler> logger)
    : ICommandHandler<CreateAntibioticCommand, Result<CreateAntibioticResult>>
{
    public async Task<Result<CreateAntibioticResult>> HandleAsync(CreateAntibioticCommand command, CancellationToken cancellationToken = default)
    {
        // Check if antibiotic group exists
        var group = await context.AntibioticGroups
            .FirstOrDefaultAsync(x => x.Id == command.AntibioticGroupId, cancellationToken);
        if (group is null)
        {
            logger.LogDebug("Antibiotic group ID not found for antibiotic group: {Id}", command.AntibioticGroupId);
            return Result<CreateAntibioticResult>.Failure(new Error(Status.BadRequest, "Antibiotic group ID not exists"));
        }

        // Map command to model
        var antibiotic = mapper.ToModel(command);

        // Save changes to database
        await context.Antibiotics.AddAsync(antibiotic, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return Result<CreateAntibioticResult>.Success(Status.Created, new CreateAntibioticResult(antibiotic.Id));
    }
}
