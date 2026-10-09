using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Antibiotics.CreateAntibiotic
{
    public class CreateAntibioticHandler(
        IDbContext context,
        ICreateMapper<CreateAntibioticCommand, Antibiotic> mapper,
        ILogger<CreateAntibioticHandler> logger)
        : ICommandHandler<CreateAntibioticCommand, Result<CreateAntibioticResult>>
    {
        public async Task<Result<CreateAntibioticResult>> HandleAsync(CreateAntibioticCommand command, CancellationToken cancellationToken = default)
        {
            // Check if antibiotic group exists
            var group = await context.AntibioticGroups
                .FirstOrDefaultAsync(x => x.Id == command.AntibioticGroupId, cancellationToken);
            if (group is null)
            {
                logger.LogDebug("Antibiotic group ID not found for antibiotic group: {Id}", command.AntibioticGroupId);
                return Result<CreateAntibioticResult>.Failure(new Error(ApplicationStatus.BadRequest, "Antibiotic group ID not exists"));
            }

            // Map command to model
            var mapResult = mapper.ToModel(command);
            if (mapResult.IsFailure())
            {
                logger.LogDebug("Failed to map command to model: {Error}", mapResult.Error);
                return Result<CreateAntibioticResult>.Failure(mapResult.Error!);
            }
            var antibiotic = mapResult.Data!;

            // Save changes to database
            await context.Antibiotics.AddAsync(antibiotic, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result<CreateAntibioticResult>.Success(ApplicationStatus.Created, new CreateAntibioticResult(antibiotic.Id));
        }
    }
}
