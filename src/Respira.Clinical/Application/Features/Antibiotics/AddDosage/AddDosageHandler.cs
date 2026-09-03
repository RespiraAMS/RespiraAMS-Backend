using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Antibiotics.AddDosage;

public class AddDosageHandler(
    IDbContext context,
    ICreateMapper<Dosage, AddDosageCommand> mapper,
    ILogger<AddDosageHandler> logger)
    : ICommandHandler<AddDosageCommand, Respira.ServiceDefaults.Contracts.Results.Result<AddDosageResult>>
{
    public async Task<Respira.ServiceDefaults.Contracts.Results.Result<AddDosageResult>> HandleAsync(AddDosageCommand command, CancellationToken cancellationToken = default)
    {
        // Check if antibiotic exists
        var antibiotic = await context.Antibiotics
            .Include(x => x.Dosages)
            .FirstOrDefaultAsync(x => x.Id == command.AntibioticId, cancellationToken);
        if (antibiotic is null)
        {
            logger.LogDebug("Antibiotic not found: {Id}", command.AntibioticId);
            return Respira.ServiceDefaults.Contracts.Results.Result<AddDosageResult>.Failure(new Error(Status.BadRequest, "Antibiotic not found"));
            // throw new NotFoundException(nameof(Antibiotic), command.AntibioticId);
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

        // Validate dosage
        var validationResult = Antibiotic.IsAntibioticDosageValid(dosages);
        if (!validationResult.IsSuccess)
        {
            logger.LogDebug("Dosage validation failed: {msg}", validationResult.Error);
            return Respira.ServiceDefaults.Contracts.Results.Result<AddDosageResult>.Failure(validationResult.Error!);
        }

        // Add the new created dosage into database and link it to antibiotic
        await context.Dosages.AddAsync(dosage, cancellationToken);
        antibiotic.DosageIds.Add(dosage.Id);
        antibiotic.Dosages.Add(dosage);

        // Save changes to database
        await context.SaveChangesAsync(cancellationToken);
        return Respira.ServiceDefaults.Contracts.Results.Result<AddDosageResult>.Success(Status.Created, new AddDosageResult(dosage.Id));
    }
}
