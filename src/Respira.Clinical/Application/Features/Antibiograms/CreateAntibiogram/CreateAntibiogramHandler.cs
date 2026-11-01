using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Antibiograms.CreateAntibiogram
{
    public class CreateAntibiogramHandler(
        IDbContext context,
        ICreateMapper<CreateAntibiogramCommand, Antibiogram> mapper,
        ILogger<CreateAntibiogramHandler> logger)
        : ICommandHandler<CreateAntibiogramCommand, Result<CreateAntibiogramResult>>
    {
        public async Task<Result<CreateAntibiogramResult>> HandleAsync(CreateAntibiogramCommand command, CancellationToken cancellationToken = default)
        {
            // Check if pathogen ID exists
            if (!await context.Pathogens.AnyAsync(x => x.Id == command.PathogenId, cancellationToken))
            {
                logger.LogDebug("Pathogen ID not found: {Id}", command.PathogenId);
                return Result<CreateAntibiogramResult>.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Pathogen not found"));
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
                return Result<CreateAntibiogramResult>.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Not all antibiotic exist"));
            }

            // Map command to model
            var mapResult = mapper.ToModel(command);
            if (mapResult.IsFailure())
            {
                logger.LogDebug("Failed to map command to model: {Error}", mapResult.Error);
                return Result<CreateAntibiogramResult>.Failure(mapResult.Error!);
            }
            var antibiogram = mapResult.Data!;
            context.UpdateRelations(antibiogram.Antibiotics, command.AntibioticIds);

            // Since the list in antibiogram has an OR semantic, we won't check anything here,
            // unlike the treatment protocol where list has AND semantic

            await context.Antibiograms.AddAsync(antibiogram, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result<CreateAntibiogramResult>.Success(ApplicationStatus.Created, new CreateAntibiogramResult(antibiogram.Id));
        }
    }
}
