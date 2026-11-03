using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Patient.Application.Contracts.Data;
using Respira.Patient.Domain.Entities;
using Respira.Patient.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine.Attributes;

namespace Respira.Patient.Application.Features.TreatmentPlans.UpsertTreatmentPlan
{
    /*
     * NOTE: this handler should be called by the confirm treatment in clinical
     * service, and shouldn't be public, which means that diagnosis or medicine
     * is already validated in the caller, so we won't validate them by calling
     * to clinical service. Doctor ID also receive from gateway, so we also won't
     * validate as well
     */

    [Middleware(typeof(UpsertTreatmentPlanMiddleware))]
    public class UpsertTreatmentPlanHandler(IDbContext context, ILogger<UpsertTreatmentPlanHandler> logger)
        : ICommandHandler<UpsertTreatmentPlanCommand, Result<UpsertTreatmentPlanResult>>
    {
        public async Task<Result<UpsertTreatmentPlanResult>> HandleAsync(UpsertTreatmentPlanCommand command, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Start upsert treatment plan");

            // Check if patient ID exists
            if (!await context.Patients.AnyAsync(x => x.Id == command.PatientId, cancellationToken))
            {
                logger.LogDebug("Patient not found: {PatientId}", command.PatientId);
                return Result<UpsertTreatmentPlanResult>.Failure(new ApplicationError(
                    ApplicationStatus.BadRequest,
                    "Patient not found"));
            }

            // Get treatment plan by patient ID and diagnosis ID
            var plan = await context.TreatmentPlans
                .Where(x => x.PatientId == command.PatientId && x.DiagnosisId == command.DiagnosisId)
                .FirstOrDefaultAsync(cancellationToken);
            if (plan is null)
            {
                logger.LogDebug("Treatment plan does not exist, created: {Detail}", new
                {
                    command.PatientId,
                    command.DiagnosisId
                });

                // Create treatment plan
                plan = new TreatmentPlan
                {
                    DoctorId = command.DoctorId,
                    PatientId = command.PatientId,
                    DiagnosisId = command.DiagnosisId,
                    Diagnosis = command.Diagnosis,
                    Medicines = command.Medicines,
                    ReasonForMedicineChoice = command.ReasonForMedicineChoice,
                    AdherenceState = command.IsAdhere ? AdherenceState.Adhere : AdherenceState.NotAdhere,
                    Status = TreatmentPlanStatus.InProgress,
                };
                await context.TreatmentPlans.AddAsync(plan, cancellationToken);
            }
            else
            {
                // If found, check if this is a close case 
                if (plan.Status == TreatmentPlanStatus.Closed)
                {
                    logger.LogDebug("Cannot update a treatment plan that was closed");
                    return Result<UpsertTreatmentPlanResult>.Failure(new ApplicationError(
                        ApplicationStatus.BadRequest,
                        "Cannot update a treatment plan that was closed"));
                }
                plan.DoctorId = command.DoctorId;
                plan.Diagnosis = command.Diagnosis;
                plan.Medicines = command.Medicines;
                plan.ReasonForMedicineChoice = command.ReasonForMedicineChoice;
                plan.AdherenceState = command.IsAdhere ? AdherenceState.Adhere : AdherenceState.NotAdhere;
                plan.Status = TreatmentPlanStatus.InProgress;
                plan.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await context.SaveChangesAsync(cancellationToken);
            return Result<UpsertTreatmentPlanResult>.Success(
                ApplicationStatus.Success,
                new UpsertTreatmentPlanResult(plan.Id));
        }
    }
}
