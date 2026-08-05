using Application.Contracts.Data;
using Microsoft.Extensions.Logging;

namespace Application.Features.Pathogens.CreatePathogen;

public class CreatePathogenHandler(
    IDbContext context,
    ICreateMapper<Pathogen, CreatePathogenCommand> mapper,
    ILogger<CreatePathogenHandler> logger)
    : ICommandHandler<CreatePathogenCommand, CreatePathogenResult>
{
    public async Task<CreatePathogenResult> HandleAsync(CreatePathogenCommand command,
        CancellationToken cancellationToken = default)
    {
        // Map command to model
        var pathogen = mapper.ToModel(command);

        // Save changes to database
        await context.Pathogens.AddAsync(pathogen, cancellationToken);
        if (await context.SaveChangesAsync(cancellationToken) <= 0)
        {
            logger.LogError("Failed to create pathogen");
            throw new ServerException();
        }

        return new CreatePathogenResult(pathogen.Id);
    }
}