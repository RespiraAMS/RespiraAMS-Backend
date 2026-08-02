using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.IcuHospitalizeCriteria.CreateIcuHospitalizeCriterion;

public class CreateIcuHospitalizeCriterionHandler(
    IDbContext context,
    ICreateMapper<IcuHospitalizeCriterion, CreateIcuHospitalizeCriterionCommand> mapper,
    ILogger<CreateIcuHospitalizeCriterionHandler> logger)
    : ICommandHandler<CreateIcuHospitalizeCriterionCommand, CreateIcuHospitalizeCriterionResult>
{
    public async Task<CreateIcuHospitalizeCriterionResult> HandleAsync(CreateIcuHospitalizeCriterionCommand command,
        CancellationToken cancellationToken = default)
    {
        // Check if disease exists
        var disease = await context.Diseases
            .FirstOrDefaultAsync(x => x.Id == command.DiseaseId, cancellationToken);
        if (disease is null)
        {
            logger.LogDebug("Disease ID not found: {Id}", command.DiseaseId);
            throw new BadRequestException("Disease ID not exists");
        }

        // Map from command to model
        var icu = mapper.ToModel(command);

        // Save changes to database
        await context.IcuHospitalizeCriteria.AddAsync(icu, cancellationToken);
        if (await context.SaveChangesAsync(cancellationToken) <= 0)
        {
            logger.LogError("Failed to save disease's ICU hospitalize criterion");
            throw new ServerException();
        }

        return new CreateIcuHospitalizeCriterionResult(icu.Id);
    }
}