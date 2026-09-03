using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.IcuHospitalizeCriteria.CreateIcuHospitalizeCriterion;

public class CreateIcuHospitalizeCriterionHandler(
    IDbContext context,
    ICreateMapper<IcuHospitalizeCriterion, CreateIcuHospitalizeCriterionCommand> mapper,
    ILogger<CreateIcuHospitalizeCriterionHandler> logger)
    : ICommandHandler<CreateIcuHospitalizeCriterionCommand, Respira.ServiceDefaults.Contracts.Results.Result<CreateIcuHospitalizeCriterionResult>>
{
    public async Task<Respira.ServiceDefaults.Contracts.Results.Result<CreateIcuHospitalizeCriterionResult>> HandleAsync(CreateIcuHospitalizeCriterionCommand command,
        CancellationToken cancellationToken = default)
    {
        // Check if disease exists
        var disease = await context.Diseases
            .FirstOrDefaultAsync(x => x.Id == command.DiseaseId, cancellationToken);
        if (disease is null)
        {
            logger.LogDebug("Disease ID not found: {Id}", command.DiseaseId);
            return Respira.ServiceDefaults.Contracts.Results.Result<CreateIcuHospitalizeCriterionResult>.Failure(new Error(Status.BadRequest, "Disease ID not found"));
            // throw new BadRequestException("Disease ID not exists");
        }

        // Map from command to model
        var icu = mapper.ToModel(command);

        // Save changes to database
        await context.IcuHospitalizeCriteria.AddAsync(icu, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Respira.ServiceDefaults.Contracts.Results.Result<CreateIcuHospitalizeCriterionResult>.Success(Status.Created, new CreateIcuHospitalizeCriterionResult(icu.Id));
    }
}
