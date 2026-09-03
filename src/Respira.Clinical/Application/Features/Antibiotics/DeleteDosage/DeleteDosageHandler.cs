using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Antibiotics.DeleteDosage;

public class DeleteDosageHandler(
    IDbContext context,
    ILogger<DeleteDosageHandler> logger)
    : ICommandHandler<DeleteDosageCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteDosageCommand command, CancellationToken cancellationToken = default)
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
        if (dosages.FirstOrDefault(d => d.Id == command.Id) is null)
        {
            logger.LogDebug("Dosage with this ID ({DosageId}) not found in this antibiotic ({AntibioticId})", command.Id, command.AntibioticId);
            return Result.Failure(new Error(Status.BadRequest, $"No dosage with this ID found in antibiotic {command.AntibioticId}"));
        }

        // Try to remove the dosage from cloned object
        dosages.RemoveAll(d => d.Id == command.Id);

        // Validate dosage
        var validationResult = Antibiotic.IsAntibioticDosageValid(dosages);
        if (!validationResult.IsSuccess())
        {
            logger.LogDebug("Dosage validation failed: {msg}", validationResult.Error);
            return Result.Failure(validationResult.Error!);
        }

        // Since we hard delete in the memory clone while our db delete is soft delete, 
        // no need to keep from cloned back to tracked entity
        var dosage = antibiotic.Dosages.First(d => d.Id == command.Id);
        dosage.IsDeleted = true;
        dosage.DeletedAt = DateTimeOffset.UtcNow;

        // No need to clean up the DosageIds and Dosages: since this is reference
        // by FK, if using RemoveAll, EF Core will try to hard delete the record,
        // which is incorrect with our soft delete setup. The global query filter
        // will just hide the soft deleted dosage anyway

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(Status.Deleted);
    }
}
