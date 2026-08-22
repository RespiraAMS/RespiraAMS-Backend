using Application.Contracts.Data;
using Application.Features.Patients.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Patients.UpdatePatient;

public class UpdatePatientHandler(IDbContext context, ILogger<UpdatePatientHandler> logger)
    : ICommandHandler<UpdatePatientCommand>
{
    public async Task HandleAsync(UpdatePatientCommand command, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("????");

        // Get entity by ID
        var patient = await context.Patients
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (patient is null)
        {
            logger.LogDebug("Patient with this ID not found: {Id}", command.Id);
            throw new NotFoundException(nameof(Patient), command.Id);
        }

        // Check if this patient already receive any treatment.
        // Because patient date of birth (which is patient age) and gender does affect diagnosis result, 
        // to keep data consistency, we won't update these values. If no treatment actually started
        // (for example, doctor enter wrong data and immediately notice), dob and gender can still be updated.
        // This is also why this feature doesn't have a mapper
        if (await context.Treatments.AnyAsync(x => x.PatientId == patient.Id, cancellationToken))
        {
            logger.LogWarning("Patient {Id} already have treatment, date of birth and gender won't update", patient.Id);

            // Map command to model
            patient.FullName = PatientNameNormalizer.Normalize(command.FullName);
            patient.MedicalRecordCode = command.MedicalRecordCode;
            patient.HealthInsuranceCardNumber = command.HealthInsuranceCardNumber;
            patient.Address = command.Address;
        }
        else
        {
            logger.LogDebug("Patient {Id} does not have treatment, update normally", patient.Id);

            // Map command to model
            patient.FullName = PatientNameNormalizer.Normalize(command.FullName);
            patient.DateOfBirth = command.DateOfBirth;
            patient.IsMale = command.IsMale;
            patient.MedicalRecordCode = command.MedicalRecordCode;
            patient.HealthInsuranceCardNumber = command.HealthInsuranceCardNumber;
            patient.Address = command.Address;
        }

        // Save changes to database
        patient.UpdatedAt = DateTimeOffset.UtcNow;
        if (await context.SaveChangesAsync(cancellationToken) <= 0)
        {
            logger.LogError("Failed to save patient to database");
            throw new ServerException();
        }
    }
}