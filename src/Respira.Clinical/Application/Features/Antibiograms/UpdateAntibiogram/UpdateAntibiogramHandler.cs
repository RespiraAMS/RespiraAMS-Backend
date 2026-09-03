using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Antibiograms.UpdateAntibiogram;

public class UpdateAntibiogramHandler(
    IDbContext context,
    IUpdateMapper<Antibiogram, UpdateAntibiogramCommand> mapper,
    ILogger<UpdateAntibiogramHandler> logger)
    : ICommandHandler<UpdateAntibiogramCommand, Respira.ServiceDefaults.Contracts.Results.Result>
{
    public async Task<Respira.ServiceDefaults.Contracts.Results.Result> HandleAsync(UpdateAntibiogramCommand command, CancellationToken cancellationToken = default)
    {
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
            return Respira.ServiceDefaults.Contracts.Results.Result.Failure(new Error(Status.BadRequest, "Not all antibiotic IDs exist"));
            // throw new BadRequestException("Not all antibiotic IDs exist");
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
            return Respira.ServiceDefaults.Contracts.Results.Result.Failure(new Error(Status.BadRequest, "Antibiogram not found"));
            // throw new NotFoundException(nameof(Antibiogram), command.Id);
        }

        // Map from command to model
        mapper.MapModel(antibiogram, command);
        context.UpdateRelations(antibiogram.Mics, command.MicIds);
        context.UpdateRelations(antibiogram.FirstPriorityMedicines, command.FirstPriorityMedicineIds);
        context.UpdateRelations(antibiogram.SecondPriorityMedicines, command.SecondPriorityMedicineIds);

        // Save changes to database
        await context.SaveChangesAsync(cancellationToken);
        return Respira.ServiceDefaults.Contracts.Results.Result.Success(Status.Updated);
    }
}
