using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Antibiotics.AddDosage
{
    public class AddDosageHandler(
        IDbContext context,
        ICreateMapper<AddDosageCommand, Dosage> mapper,
        ILogger<AddDosageHandler> logger) : ICommandHandler<AddDosageCommand, Result<AddDosageResult>>
    {
        public async Task<Result<AddDosageResult>> HandleAsync(AddDosageCommand command, CancellationToken cancellationToken = default)
        {
            // Check if antibiotic exists
            var antibiotic = await context.Antibiotics
                .Include(x => x.Dosages)
                .FirstOrDefaultAsync(x => x.Id == command.AntibioticId, cancellationToken);
            if (antibiotic is null)
            {
                logger.LogDebug("Antibiotic not found: {Id}", command.AntibioticId);
                return Result<AddDosageResult>.Failure(new Error(ApplicationStatus.BadRequest, "Antibiotic not found"));
            }

            // Map from command to entity
            var mapResult = mapper.ToModel(command);
            if (mapResult.IsFailure())
            {
                logger.LogDebug("Failed to map command to model: {Error}", mapResult.Error);
                return Result<AddDosageResult>.Failure(mapResult.Error!);
            }
            var dosage = mapResult.Data!;

            // Try adding dosage into cloned and check for business validation
            var dosages = antibiotic.Dosages.Select(d => new Dosage() // Deep copy to avoid EF tracking issue
            {
                Id = d.Id,
                AntibioticId = d.AntibioticId,
                Dose = d.Dose,
                RouteOfAdministration = d.RouteOfAdministration,
                Crcl = d.Crcl // Since this is not an entity registered in EF Core, a direct copy wouldn't cause issues
            });
            dosages = dosages.Append(dosage);

            // Validate dosage
            var validationResult = Antibiotic.IsAntibioticDosageValid([.. dosages]);
            if (!validationResult.IsSuccess())
            {
                logger.LogDebug("Dosage validation failed: {msg}", validationResult.Error);
                return Result<AddDosageResult>.Failure(validationResult.Error!);
            }

            // Add the new created dosage into database and link it to antibiotic
            await context.Dosages.AddAsync(dosage, cancellationToken);
            antibiotic.Dosages.Add(dosage);

            // Save changes to database
            await context.SaveChangesAsync(cancellationToken);
            return Result<AddDosageResult>.Success(ApplicationStatus.Created, new AddDosageResult(dosage.Id));
        }
    }
}
