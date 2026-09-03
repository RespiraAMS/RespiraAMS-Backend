using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.ResistanceRiskFactors.CreateResistanceRiskFactor;

public class CreateResistanceRiskFactorHandler(
    IDbContext context,
    ICreateMapper<ResistanceRiskFactor, CreateResistanceRiskFactorCommand> mapper,
    ILogger<CreateResistanceRiskFactorCommand> logger)
    : ICommandHandler<CreateResistanceRiskFactorCommand, Result<CreateResistanceRiskFactorResult>>
{
    public async Task<Result<CreateResistanceRiskFactorResult>> HandleAsync(CreateResistanceRiskFactorCommand command,
        CancellationToken cancellationToken = default)
    {
        // Check if disease exists
        var disease = await context.Diseases
            .FirstOrDefaultAsync(x => x.Id == command.DiseaseId, cancellationToken);
        if (disease is null)
        {
            logger.LogDebug("Disease ID not found: {Id}", command.DiseaseId);
            return Result<CreateResistanceRiskFactorResult>.Failure(new Error(Status.BadRequest, "Disease ID not exists"));
        }

        // Check if pathogen exists
        var pathogen = await context.Pathogens
            .FirstOrDefaultAsync(x => x.Id == command.PathogenId, cancellationToken);
        if (pathogen is null)
        {
            logger.LogDebug("Pathogen ID not found: {Id}", command.PathogenId);
            return Result<CreateResistanceRiskFactorResult>.Failure(new Error(Status.BadRequest, "Pathogen ID not exists"));
        }

        // Map from command to query
        var factor = mapper.ToModel(command);

        // Save changes to database
        await context.ResistanceRiskFactors.AddAsync(factor, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result<CreateResistanceRiskFactorResult>.Success(Status.Created, new CreateResistanceRiskFactorResult(factor.Id));
    }
}
