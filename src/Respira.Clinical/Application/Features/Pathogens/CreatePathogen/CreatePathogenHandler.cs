using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Pathogens.CreatePathogen
{
    public class CreatePathogenHandler(
        IDbContext context,
        ICreateMapper<CreatePathogenCommand, Pathogen> mapper,
        ILogger<CreatePathogenHandler> logger)
        : ICommandHandler<CreatePathogenCommand, Result<CreatePathogenResult>>
    {
        public async Task<Result<CreatePathogenResult>> HandleAsync(CreatePathogenCommand command, CancellationToken cancellationToken = default)
        {
            // Map command to model
            var mapResult = mapper.ToModel(command);
            if (mapResult.IsFailure())
            {
                logger.LogDebug("Failed to map command to model: {Error}", mapResult.Error);
                return Result<CreatePathogenResult>.Failure(mapResult.Error!);
            }
            var pathogen = mapResult.Data!;

            // Save changes to database
            await context.Pathogens.AddAsync(pathogen, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);


            return Result<CreatePathogenResult>.Success(ApplicationStatus.Created, new CreatePathogenResult(pathogen.Id));
        }
    }
}
