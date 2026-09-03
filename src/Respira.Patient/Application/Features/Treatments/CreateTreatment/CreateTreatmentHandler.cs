using Application.Contracts.Data;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Application.Features.Treatments.CreateTreatment;

public class CreateTreatmentHandler(
    IMessageBus bus,
    IDbContext context,
    ICreateMapper<Treatment, CreateTreatmentCommand> mapper,
    IMapper<DiagnosisRecord, ValidateDiagnosisQuery> validateDiagnoseResultMapper,
    ILogger<CreateTreatmentHandler> logger)
    : ICommandHandler<CreateTreatmentCommand, CreateTreatmentResult>
{
    public async Task<CreateTreatmentResult> HandleAsync(CreateTreatmentCommand command, CancellationToken cancellationToken = default)
    {
        // Validate the diagnosis record with Clinical service
        var validateResult = await bus.InvokeAsync<Result<ValidateDiagnosisResult>>(validateDiagnoseResultMapper.Map(command.DiagnosisRecord), cancellationToken);
        if (validateResult.IsFailure())
        {
            logger.LogWarning("Failed to validate diagnosis result: {Error}", validateResult.Error);
            throw new ServerException();
        }
        
        // Check if data is valid
        if (!validateResult.Data!.IsValid)
        {
            logger.LogDebug("Invalid diagnosis record: {Message}", validateResult.Data.Message);
            throw new BadRequestException($"Invalid diagnosis record: {validateResult.Data.Message}");
        }

        // When a treatment is created, there are 2 cases:
        // 1. This is the first treatment for this patient: proceed as normal
        // 2. There are already treatments for this patient: then we need to
        // update the last treatment status to poor response, because only when
        // the patient has poor response that we would want to change medicines
        // (even if we have microbiological test result)
        var lastTreatment = await context.Treatments
            .Where(x => x.PatientId == command.PatientId)
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
