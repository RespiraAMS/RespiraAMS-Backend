using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Patient.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Features.Patients.DischargePatient
{
    public class DischargePatientHandler(IDbContext context, ILogger<DischargePatientHandler> logger)
        : ICommandHandler<DischargePatientCommand, Result>
    {
        public async Task<Result> HandleAsync(DischargePatientCommand command, CancellationToken cancellationToken = default)
        {
            // Get patient by ID
            var patient = await context.Patients
                .Include(p => p.TreatmentPlans)
                .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
            if (patient is null)
            {
                logger.LogDebug("Patient not found: {Id}", command.Id);
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Patient not found"));
            }

            // Only discharge patient if they have receive at least one treatment
            if (patient.TreatmentPlans.Count == 0)
            {
                logger.LogDebug("Patient does not have any treatment, cannot discharge");
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Patient does not have any treatment, cannot discharge"));
            }

            // A patient already discharge shouldn't be discharged again
            if (patient.Discharge is not null)
            {
                logger.LogDebug("Patient already been discharged: {PatientId}", command.Id);
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Patient has already been discharged, cannot discharge"));
            }

            // Discharge patient.
            // NOTE: even though the TreatmentOutcome does have the OutOfScope value,
            // which doesn't fit the concept of discharge in clinical context (since without antibiotic
            // treatment, the hospital still treat them), but since our application doesn't
            // handle such case, in the persepective of our system, it can still be counted as
            // discharge.
            patient.Discharge = DateTimeOffset.UtcNow;
            patient.TreatmentOutcome = command.Outcome;
            patient.UpdatedAt = DateTimeOffset.UtcNow;

            // Save changes to database
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(ApplicationStatus.Updated);
        }
    }
}
