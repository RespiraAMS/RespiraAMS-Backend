using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Patient.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Features.Patients.DeletePatient
{
    public class DeletePatientHandler(IDbContext context, ILogger<DeletePatientHandler> logger)
        : ICommandHandler<DeletePatientCommand, Result>
    {
        public async Task<Result> HandleAsync(DeletePatientCommand command, CancellationToken cancellationToken = default)
        {
            // Get patient by ID
            var patient = await context.Patients
                .Include(p => p.Treatments)
                .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
            if (patient is null)
            {
                logger.LogDebug("Patient not found: {Id}", command.Id);
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Patient not found"));
            }

            // Only delete patient if they have receive no treatment
            // (e.g. doctor input wrong and want to delete)
            if (patient.Treatments.Count > 0)
            {
                logger.LogDebug("Patient already received treatments, cannot delete: {Id}", command.Id);
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Patient already received treatments, cannot delete"));
            }

            // Delete patient (and cascade its next of kin if any)
            await context.ExecuteInTransactionAsync(async () =>
            {
                // Delete patient
                patient.IsDeleted = true;
                patient.DeletedAt = DateTimeOffset.UtcNow;

                // Cascade delete next of kin
                var nextOfKinCount = await context.NextOfKins
                    .Where(x => x.PatientId == patient.Id)
                    .ExecuteUpdateAsync(x => x
                        .SetProperty(nok => nok.IsDeleted, true)
                        .SetProperty(nok => nok.DeletedAt, DateTimeOffset.UtcNow), cancellationToken);

                logger.LogDebug("Deleted patient: {detail}", new
                {
                    PatientId = patient.Id,
                    NextOfKinCount = nextOfKinCount
                });

            }, cancellationToken);

            return Result.Success(ApplicationStatus.Deleted);
        }
    }
}
