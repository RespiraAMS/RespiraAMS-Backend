using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Antibiograms.UpdateAntibiogram
{
    public class UpdateAntibiogramHandler(
        IDbContext context,
        IUpdateMapper<Antibiogram, UpdateAntibiogramCommand> mapper,
        ILogger<UpdateAntibiogramHandler> logger)
        : ICommandHandler<UpdateAntibiogramCommand, Result>
    {
        public async Task<Result> HandleAsync(UpdateAntibiogramCommand command, CancellationToken cancellationToken = default)
        {
            // Get antibiogram by ID
            var antibiogram = await context.Antibiograms
                .Include(x => x.Antibiotics)
                .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
            if (antibiogram is null)
            {
                logger.LogDebug("Antibiogram not found: {Id}", command.Id);
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Antibiogram not found"));
            }

            // Check if pathogen ID exists
            if (!await context.Pathogens.AnyAsync(x => x.Id == command.PathogenId, cancellationToken))
            {
                logger.LogDebug("Pathogen ID not found: {Id}", command.PathogenId);
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Pathogen not found"));
            }

            // Check if all antibiotics exist
            var antibioticDbCount = await context.Antibiotics
                .CountAsync(x => command.AntibioticIds.Contains(x.Id), cancellationToken);
            if (antibioticDbCount != command.AntibioticIds.Count)
            {
                logger.LogDebug("Not all antibiotic IDs exist in database: {Detail}", new
                {
                    AntibioticDbCount = antibioticDbCount,
                    AntibioticProvided = command.AntibioticIds.Count
                });
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Not all antibiotic exist"));
            }

            // Map command to model
            var mapResult = mapper.MapModel(antibiogram, command);
            if (mapResult.IsFailure())
            {
                logger.LogDebug("Failed to map command to model: {Error}", mapResult.Error);
                return Result.Failure(mapResult.Error!);
            }
            context.UpdateRelations(antibiogram.Antibiotics, command.AntibioticIds);

            // Since the list in antibiogram has an OR semantic, we won't check anything here,
            // unlike the treatment protocol where list has AND semantic

            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(ApplicationStatus.Updated);
        }
    }
}
