using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Patients.DischargePatient;

public class DischargePatientHandler(
    IDbContext context,
    IUpdateMapper<Patient, DischargePatientCommand> mapper,
    ILogger<DischargePatientHandler> logger)
    : ICommandHandler<DischargePatientCommand, Result>
{
    public async Task<Result> HandleAsync(DischargePatientCommand command, CancellationToken cancellationToken = default)
    {
        // Get entity by ID
        var patient = await context.Patients.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (patient is null)
        {
            logger.LogDebug("Patient with this ID not found: {Id}", command.Id);
            return Result.Failure(new Error(Status.BadRequest, "Patient not found"));
        }

        // Check if patient has any treatment
        if (!await context.Treatments.AnyAsync(x => x.PatientId == patient.Id, cancellationToken))
        {
            logger.LogDebug("Patient {Id} has no treatments, cannot discharged", command.Id);
            return Result.Failure(new Error(Status.BusinessRuleViolation, "Patient has no treatments, cannot discharged"));
        }

        // Map command to model
        mapper.MapModel(patient, command);

        // Save changes to database
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(Status.Updated);
    }
}
