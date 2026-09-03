using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Antibiotics.CreateAntibiotic;

public class CreateAntibioticHandler(
    IDbContext context,
    ICreateMapper<Antibiotic, CreateAntibioticCommand> mapper,
    ILogger<CreateAntibioticHandler> logger)
    : ICommandHandler<CreateAntibioticCommand, Respira.ServiceDefaults.Contracts.Results.Result<CreateAntibioticResult>>
{
    public async Task<Respira.ServiceDefaults.Contracts.Results.Result<CreateAntibioticResult>> HandleAsync(CreateAntibioticCommand command, CancellationToken cancellationToken = default)
    {
        // Check if antibiotic group exists
        var group = await context.AntibioticGroups
            .FirstOrDefaultAsync(x => x.Id == command.AntibioticGroupId, cancellationToken);
        if (group is null)
        {
            logger.LogDebug("Antibiotic group ID not found for antibiotic group: {Id}", command.AntibioticGroupId);
            return Respira.ServiceDefaults.Contracts.Results.Result<CreateAntibioticResult>.Failure(new Error(Status.BadRequest, "Antibiotic group ID not exists"));
            // throw new BadRequestException("Antibiotic group ID not exists");
        }

        // Map command to model
        var antibiotic = mapper.ToModel(command);

        // Save changes to database
        await context.Antibiotics.AddAsync(antibiotic, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return Respira.ServiceDefaults.Contracts.Results.Result<CreateAntibioticResult>.Success(Status.Created, new CreateAntibioticResult(antibiotic.Id));
    }
}
