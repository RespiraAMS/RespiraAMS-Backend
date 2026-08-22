using Application.Contracts.Data;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Treatments.CreateTreatment;

public class CreateTreatmentHandler(
    IDbContext context,
    ICreateMapper<Treatment, CreateTreatmentCommand> mapper,
    ILogger<CreateTreatmentHandler> logger)
    : ICommandHandler<CreateTreatmentCommand, CreateTreatmentResult>
{
    public async Task<CreateTreatmentResult> HandleAsync(CreateTreatmentCommand command, CancellationToken cancellationToken = default)
    {
        // When a treatment is created, there are 2 cases:
        // 1. This is the first treatment for this patient: proceed as normal
        // 2. There are already treatments for this patient: then we need to
        // update the last treatment status to poor response, because only when
        // the patient has poor response that we would want to change medicines
        // (even if we have microbiological test result)
        var lastTreatment = await context.Treatments
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (lastTreatment is not null)
        {
            logger.LogDebug("Patient has already had treatments, updating last treatment status to poor response before switching to new treatment");
            lastTreatment.Status = PatientTreatmentStatus.PoorResponse;
        }

        // Map from command to model
        var treatment = mapper.ToModel(command);

        // Save to database
        await context.Treatments.AddAsync(treatment, cancellationToken);
        if (await context.SaveChangesAsync(cancellationToken) <= 0)
        {
            logger.LogError("Failed to save treatment to database");
            throw new ServerException();
        }

        return new CreateTreatmentResult(treatment.Id);
    }
}
