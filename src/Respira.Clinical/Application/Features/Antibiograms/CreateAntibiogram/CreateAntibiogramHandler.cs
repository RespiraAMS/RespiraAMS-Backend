using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Antibiograms.CreateAntibiogram;

public class CreateAntibiogramHandler(
    IDbContext context,
    ICreateMapper<Antibiogram, CreateAntibiogramCommand> mapper,
    ILogger<CreateAntibiogramHandler> logger)
    : ICommandHandler<CreateAntibiogramCommand, CreateAntibiogramResult>
{
    public async Task<CreateAntibiogramResult> HandleAsync(CreateAntibiogramCommand command,
        CancellationToken cancellationToken = default)
    {
        // Check if pathogen ID exists
        var pathogen = await context.Pathogens
            .FirstOrDefaultAsync(x => x.Id == command.PathogenId, cancellationToken);
        if (pathogen is null)
        {
            logger.LogDebug("Pathogen with ID not found: {Id}", command.PathogenId);
            throw new BadRequestException("Pathogen with ID not found");
        }

        // Check if all antibiotics exists
        var ids = command.MicIds
            .Concat(command.FirstPriorityMedicineIds)
            .Concat(command.SecondPriorityMedicineIds)
            .Distinct();
        var allAntibioticsExist = await context.Antibiotics
            .CountAsync(x => ids.Contains(x.Id), cancellationToken) == ids.Count();
        if (!allAntibioticsExist)
        {
            logger.LogDebug("Not all antibiotic IDs exist");
            throw new BadRequestException("Not all antibiotic IDs exist");
        }

        // Map from command to model
        var antibiogram = mapper.ToModel(command);
        context.UpdateRelations(antibiogram.Mics, command.MicIds);
        context.UpdateRelations(antibiogram.FirstPriorityMedicines, command.FirstPriorityMedicineIds);
        context.UpdateRelations(antibiogram.SecondPriorityMedicines, command.SecondPriorityMedicineIds);

        // Save changes to database
        await context.Antibiograms.AddAsync(antibiogram, cancellationToken);
        if (await context.SaveChangesAsync(cancellationToken) <= 0)
        {
            logger.LogError("Failed to save antibiogram");
            throw new ServerException();
        }

        return new CreateAntibiogramResult(antibiogram.Id);
    }
}
