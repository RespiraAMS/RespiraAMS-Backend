using Application.Contracts.Data;
using ImTools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Antibiotics.AddDosage;

public class AddDosageHandler(
    IDbContext context,
    ICreateMapper<Dosage, AddDosageCommand> mapper,
    ILogger<AddDosageHandler> logger)
    : ICommandHandler<AddDosageCommand, AddDosageResult>
{
    public async Task<AddDosageResult> HandleAsync(AddDosageCommand command,
        CancellationToken cancellationToken = default)
    {
        // Check if antibiotic exists
        var antibiotic = await context.Antibiotics
            .FirstOrDefaultAsync(x => x.Id == command.AntibioticId, cancellationToken);
        if (antibiotic is null)
        {
            logger.LogDebug("Antibiotic not found: {Id}", command.AntibioticId);
            throw new NotFoundException(nameof(Antibiotic), command.AntibioticId);
        }

        // Map from command to entity
        var dosage = mapper.ToModel(command);

        // Add dosage to database
        await context.Dosages.AddAsync(dosage, cancellationToken);

        // Add dosage to antibiotic
        context.UpdateRelations(antibiotic.Dosages, [dosage.Id]);

        // Save changes to database
        if (await context.SaveChangesAsync(cancellationToken) <= 0)
        {
            logger.LogError("Failed to add new dosage to antibiotic");
            throw new ServerException();
        }

        return new AddDosageResult(dosage.Id);
    }
}