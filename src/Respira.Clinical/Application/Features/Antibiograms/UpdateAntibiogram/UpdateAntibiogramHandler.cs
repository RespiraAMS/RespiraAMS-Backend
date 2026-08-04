using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Antibiograms.UpdateAntibiogram;

public class UpdateAntibiogramHandler(
    IDbContext context,
    IUpdateMapper<Antibiogram, UpdateAntibiogramCommand> mapper,
    ILogger<UpdateAntibiogramHandler> logger)
    : ICommandHandler<UpdateAntibiogramCommand>
{
    public async Task HandleAsync(UpdateAntibiogramCommand command, CancellationToken cancellationToken = default)
    {
        // Check if all antibiotics exists
        var ids = command.MicIds.Concat(command.FirstPriorityMedicineIds).Concat(command.SecondPriorityMedicineIds);
        var allAntibioticsExist = await context.Antibiotics
            .CountAsync(x => ids.Contains(x.Id), cancellationToken) == ids.Count();
        if (!allAntibioticsExist)
        {
            logger.LogDebug("Not all antibiotic IDs exist");
            throw new BadRequestException("Not all antibiotic IDs exist");
        }

        // Get entity by ID
        var antibiogram = await context.Antibiograms
            .Include(x => x.Mics)
            .Include(x => x.FirstPriorityMedicines)
            .Include(x => x.SecondPriorityMedicines)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (antibiogram is null)
        {
            logger.LogDebug("Antibiogram not found: {Id}", command.Id);
            throw new NotFoundException(nameof(Antibiogram), command.Id);
        }

        // Map from command to model
        mapper.MapModel(antibiogram, command);
        context.UpdateRelations(antibiogram.Mics, command.MicIds);
        context.UpdateRelations(antibiogram.FirstPriorityMedicines, command.FirstPriorityMedicineIds);
        context.UpdateRelations(antibiogram.SecondPriorityMedicines, command.SecondPriorityMedicineIds);

        // Save changes to database
        if (await context.SaveChangesAsync(cancellationToken) <= 0)
        {
            logger.LogError("Failed to save antibiogram");
            throw new ServerException();
        }
    }
}