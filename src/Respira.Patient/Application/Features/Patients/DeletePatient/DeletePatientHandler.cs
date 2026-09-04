using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Patients.DeletePatient;

public class DeletePatientHandler(IDbContext context, ILogger<DeletePatientHandler> logger)
    : ICommandHandler<DeletePatientCommand, Result>
{
    public async Task<Result> HandleAsync(DeletePatientCommand command, CancellationToken cancellationToken = default)
    {
        // Get entity by ID
        var patient = await context.Patients.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (patient is null)
        {
            logger.LogDebug("Patient with this ID not found: {Id}", command.Id);
            return Result.Failure(new Error(Status.BadRequest, "Patient not found"));
        }

        // Check if patient has any treatment
        if (await context.Treatments.AnyAsync(x => x.PatientId == patient.Id, cancellationToken))
        {
            logger.LogDebug("Patient {Id} has already received treatment, cannot delete", command.Id);
            return Result.Failure(new Error(Status.BusinessRuleViolation, "Patient has already received treatment, cannot delete"));
        }

        // Delete patient
        patient.IsDeleted = true;
        patient.DeletedAt = DateTimeOffset.UtcNow;

        // Save changes to database
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(Status.Deleted);
    }
}
