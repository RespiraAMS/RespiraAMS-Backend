using Application.Contracts.Data;
using Application.Features.Antibiotics.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Antibiotics.DeleteDosage;

public class DeleteDosageHandler(
    IDbContext context,
    ILogger<DeleteDosageHandler> logger)
    : ICommandHandler<DeleteDosageCommand>
{
    public async Task HandleAsync(DeleteDosageCommand command, CancellationToken cancellationToken = default)
    {
        // Get antibiotic that own this dosage
        var antibiotic = await context.Antibiotics
            .Include(x => x.Dosages)
            .FirstOrDefaultAsync(x => x.Id == command.AntibioticId, cancellationToken);
        if (antibiotic is null)
        {
            logger.LogDebug("Dosage with this antibiotic not found: {AntibioticId}", command.AntibioticId);
            throw new NotFoundException(nameof(Antibiotic), command.AntibioticId);
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
            throw new NotFoundException(nameof(Dosage), command.Id);
        }

        // Try remove the dosage from cloned object
        dosages.RemoveAll(d => d.Id == command.Id);

        // Check for business logic
        try
        {
            DosageBusinessChecker.IsValidDosage(dosages);
        }
        catch (DosageEmptyException e)
        {
            logger.LogDebug("Dosage validation failed: {msg}", e.Message);
            throw new BadRequestException("Delete this dosage violate business rule: antibiotic must have at least 1 dosage");
        }
        catch (StandardDoseInvalidException e)
        {
            logger.LogDebug("Dosage validation failed: {msg}", e.Message);
            throw new BadRequestException("Delete this dosage violate business rule: each route of administration must have 1 and only 1 standard dose");
        }
        catch (OverlappedCrclException e)
        {
            logger.LogDebug("Dosage validation failed: {msg}", e.Message);
            throw new BadRequestException("Delete this dosage violate business rule: all dosages in each route of administration must not have overlapping CrCl range");
        }
        catch (Exception e)
        {
            logger.LogError("Failed to validate dosage: {exception}", e);
            throw new ServerException();
        }

        // Since we hard delete in the memory clone while our db delete is soft delete, 
        // no need to keep from cloned back to tracked entity
        var dosage = antibiotic.Dosages.First(d => d.Id == command.Id);
        dosage.IsDeleted = true;
        dosage.DeletedAt = DateTimeOffset.UtcNow;
        // antibiotic.DosageIds.RemoveAll(d => d == command.Id);
        // antibiotic.Dosages.RemoveAll(d => d.Id == command.Id);
        if (await context.SaveChangesAsync(cancellationToken) <= 0)
        {
            logger.LogError("Failed to delete antibiotic dosage");
            throw new ServerException();
        }
    }
}
