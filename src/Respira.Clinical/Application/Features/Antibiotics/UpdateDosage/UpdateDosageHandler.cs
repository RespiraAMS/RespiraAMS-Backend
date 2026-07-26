using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Antibiotics.UpdateDosage;

public class UpdateDosageHandler(
    IDbContext context,
    IUpdateMapper<Dosage, UpdateDosageCommand> mapper,
    ILogger<UpdateDosageHandler> logger)
    : ICommandHandler<UpdateDosageCommand>
{
    public async Task HandleAsync(UpdateDosageCommand command, CancellationToken cancellationToken = default)
    {
        // Get entity by ID
        var dosage = await context.Dosages
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (dosage is null)
        {
            logger.LogDebug("Antibiotic dosage not found: {Id}", command.Id);
            throw new NotFoundException(nameof(Dosage), command.Id);
        }

        // Map command to model
        mapper.MapModel(dosage, command);

        // Save changes to database
        if (await context.SaveChangesAsync(cancellationToken) <= 0)
        {
            logger.LogError("Failed to save antibiotic dosage");
            throw new ServerException();
        }
    }
}