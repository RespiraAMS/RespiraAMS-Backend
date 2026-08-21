using Application.Contracts.Data;
using Application.Features.Antibiotics.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Antibiotics.AddDosage;

public class AddDosageHandler(
    IDbContext context,
    DosageBusinessChecker checker,
    ICreateMapper<Dosage, AddDosageCommand> mapper,
    ILogger<AddDosageHandler> logger)
    : ICommandHandler<AddDosageCommand, AddDosageResult>
{
    public async Task<AddDosageResult> HandleAsync(AddDosageCommand command,
        CancellationToken cancellationToken = default)
    {
        // Check if antibiotic exists
        var antibiotic = await context.Antibiotics
            .Include(x => x.Dosages)
            .FirstOrDefaultAsync(x => x.Id == command.AntibioticId, cancellationToken);
        if (antibiotic is null)
        {
            logger.LogDebug("Antibiotic not found: {Id}", command.AntibioticId);
            throw new NotFoundException(nameof(Antibiotic), command.AntibioticId);
        }

        // Map from command to entity
        var dosage = mapper.ToModel(command);

        // Try adding dosage into cloned and check for business validation
        var dosages = antibiotic.Dosages.ConvertAll(d => new Dosage() // Deep copy to avoid EF tracking issue
        {
            Id = d.Id,
            AntibioticId = d.AntibioticId,
            Dose = d.Dose,
            RouteOfAdministration = d.RouteOfAdministration,
            Crcl = d.Crcl // Since this is not an entity registered in EF Core, a direct copy wouldn't cause issues
        });
        dosages.Add(dosage);
        try
        {
            checker.IsValidDosage(dosages);
        }
        catch (StandardDoseInvalidException e)
        {
            logger.LogDebug("Dosage validation failed: {msg}", e.Message);
            throw new BadRequestException("Adding this dosage violate business rule: each route of administration must have 1 and only 1 standard dose");
        }
        catch (OverlappedCrclException e)
        {
            logger.LogDebug("Dosage validation failed: {msg}", e.Message);
            throw new BadRequestException("Adding this dosage violate business rule: all dosages in each route of administration must not have overlapping CrCl range");
        }
        catch (Exception e)
        {
            logger.LogError("Failed to validate dosage: {exception}", e);
            throw new ServerException();
        }

        // Add the new created dosage into database and link it to antibiotic
        await context.Dosages.AddAsync(dosage, cancellationToken);
        antibiotic.DosageIds.Add(dosage.Id);
        antibiotic.Dosages.Add(dosage);

        // Save changes to database
        if (await context.SaveChangesAsync(cancellationToken) <= 0)
        {
            logger.LogError("Failed to add new dosage to antibiotic");
            throw new ServerException();
        }

        return new AddDosageResult(dosage.Id);
    }
}
