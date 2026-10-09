using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Antibiotics.UpdateDosage
{
    public class UpdateDosageHandler(
        IDbContext context,
        IUpdateMapper<Dosage, UpdateDosageCommand> mapper,
        ILogger<UpdateDosageHandler> logger) : ICommandHandler<UpdateDosageCommand, Result>
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
                return Result.Failure(new Error(ApplicationStatus.BadRequest, "Antibiotic not found"));
            }

            // Get the dosage for update from the fetched antibiotic
            var dosages = antibiotic.Dosages.Select(d => new Dosage() // Deep copy to avoid EF tracking issue
            {
                Id = d.Id,
                AntibioticId = d.AntibioticId,
                Dose = d.Dose,
                RouteOfAdministration = d.RouteOfAdministration,
                Crcl = d.Crcl // Since this is not an entity registered in EF Core, a direct copy wouldn't cause issues
            }).ToList();
            var dosage = dosages.FirstOrDefault(d => d.Id == command.Id);
            if (dosage is null)
            {
                logger.LogDebug("Dosage with this ID ({DosageId}) not found in this antibiotic ({AntibioticId})", command.Id, command.AntibioticId);
                return Result.Failure(new Error(ApplicationStatus.BadRequest, $"No dosage with this ID found in antibiotic {command.AntibioticId}"));
            }

            // Map command to model
            var mapResult = mapper.MapModel(dosage, command);
            if (mapResult.IsFailure())
            {
                logger.LogDebug("Failed to map command to model: {Error}", mapResult.Error);
                return Result.Failure(mapResult.Error!);
            }

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
            return Result.Success(ApplicationStatus.Updated);
        }
    }
}
