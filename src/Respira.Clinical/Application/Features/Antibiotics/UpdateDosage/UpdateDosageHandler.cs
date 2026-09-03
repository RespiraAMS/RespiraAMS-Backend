using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Antibiotics.UpdateDosage;

public class UpdateDosageHandler(
    IDbContext context,
    IUpdateMapper<Dosage, UpdateDosageCommand> mapper,
    ILogger<UpdateDosageHandler> logger)
    : ICommandHandler<UpdateDosageCommand, Result>
{
    public async Task<Result> HandleAsync(UpdateDosageCommand command, CancellationToken cancellationToken = default)
    {
        // Get antibiotic that own this dosage
        var antibiotic = await context.Antibiotics
            .Include(x => x.Dosages)
            .FirstOrDefaultAsync(x => x.Id == command.AntibioticId, cancellationToken);
        if (antibiotic is null)
        {
            logger.LogDebug("Dosage with this antibiotic not found: {AntibioticId}", command.AntibioticId);
            return Result.Failure(new Error(Status.BadRequest, "Antibiotic not found"));
        }

        // Get the dosage for update from the fetched antibiotic
        var dosages = antibiotic.Dosages.ConvertAll(d => new Dosage() // Deep copy to avoid EF tracking issue
        {
            Id = d.Id,
            AntibioticId = d.AntibioticId,
            Dose = d.Dose,
            RouteOfAdministration = d.RouteOfAdministration,
            Crcl = d.Crcl // Since this is not an entity registered in EF Core, a direct copy wouldn't cause issues
        });
        var dosage = dosages.FirstOrDefault(d => d.Id == command.Id);
        if (dosage is null)
        {
            logger.LogDebug("Dosage with this ID ({DosageId}) not found in this antibiotic ({AntibioticId})", command.Id, command.AntibioticId);
            return Result.Failure(new Error(Status.BadRequest, $"No dosage with this ID found in antibiotic {command.AntibioticId}"));
        }

        // Map command to model
        mapper.MapModel(dosage, command);

        // Validate dosage
        var validationResult = Antibiotic.IsAntibioticDosageValid(dosages);
        if (!validationResult.IsSuccess())
        {
            logger.LogDebug("Dosage validation failed: {msg}", validationResult.Error);
            return Result.Failure(validationResult.Error!);
        }

        // Update dosage to database
        var dbDosage = antibiotic.Dosages.First(d => d.Id == command.Id);
        mapper.MapModel(dbDosage, command);

        // Save changes to database
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(Status.Updated);
    }
}
