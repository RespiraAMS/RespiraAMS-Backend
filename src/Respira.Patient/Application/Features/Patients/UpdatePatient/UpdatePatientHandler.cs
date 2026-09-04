using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Patients.UpdatePatient;

public class UpdatePatientHandler(IDbContext context, ILogger<UpdatePatientHandler> logger)
    : ICommandHandler<UpdatePatientCommand, Result>
{
    public async Task<Result> HandleAsync(UpdatePatientCommand command, CancellationToken cancellationToken = default)
    {
        // Get entity by ID
        var patient = await context.Patients.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (patient is null)
        {
            logger.LogDebug("Patient with this ID not found: {Id}", command.Id);
            return Result.Failure(new Error(Status.BadRequest, "Patient not found"));
        }

        // Check if this patient already receive any treatment.
        // Because patient date of birth (which is patient age) and gender does affect diagnosis result,
        // to keep data consistency, we won't update these values. If no treatment actually started
        // (for example, doctor enter wrong data and immediately notice), dob and gender can still be updated.
        // This is also why this feature doesn't have a mapper
        if (!await context.Treatments.AnyAsync(x => x.PatientId == patient.Id, cancellationToken))
        {
            logger.LogDebug("Patient {Id} does not have treatment, update normally", patient.Id);

            // Map command to model
            patient.FullName = command.FullName;
            patient.DateOfBirth = command.DateOfBirth;
            patient.IsMale = command.IsMale;
        }
        else
        {
            logger.LogWarning("Patient {Id} already have treatment, date of birth and gender won't update", patient.Id);
        }

        patient.MedicalRecordCode = command.MedicalRecordCode;
        patient.HealthInsuranceCardNumber = command.HealthInsuranceCardNumber;
        patient.Address = command.Address;
        patient.City = command.City;
        patient.Country = command.Country;
        patient.UpdatedAt = DateTimeOffset.UtcNow;

        // Save changes to database
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(Status.Updated);
    }
}
