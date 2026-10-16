using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Patient.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Features.Patients.UpdatePatient
{
    public class UpdatePatientHandler(IDbContext context, ILogger<UpdatePatientHandler> logger)
        : ICommandHandler<UpdatePatientCommand, Result>
    {
        public async Task<Result> HandleAsync(UpdatePatientCommand command, CancellationToken cancellationToken = default)
        {
            // Get patient by ID
            var patient = await context.Patients
                .Include(p => p.Treatments)
                .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
            if (patient is null)
            {
                logger.LogDebug("Patient not found: {Id}", command.Id);
                return Result.Failure(new Error(ApplicationStatus.BadRequest, "Patient not found"));
            }

            // Check if the new medical record code is unique
            var sameMrc = await context.Patients
                .AnyAsync(x => x.Id != patient.Id && x.MedicalRecordCode == command.MedicalRecordCode, cancellationToken);
            if (sameMrc)
            {
                logger.LogDebug("Medical record code {code} is already existed", command.MedicalRecordCode);
                return Result.Failure(new Error(ApplicationStatus.BadRequest, "Medical record code is already existed"));
            }

            // Check if patient new age is valid
            if (!Domain.Entities.Patient.IsValidAge(command.DateOfBirth))
            {
                logger.LogDebug("Patient new date of birth violated the age constraint rule, cannot update: {detail}", new
                {
                    Dob = command.DateOfBirth,
                    Age = Domain.Entities.Patient.Age(command.DateOfBirth)
                });
                return Result.Failure(new Error(ApplicationStatus.BusinessRuleViolation, "Patient new date of birth violated the age constraint rule, cannot update"));
            }

            /*
             * Business logic for update patient: 
             * 1. Information that cannot be changed: admission must not be changed in all cases.
             * 2. Information that can be changed, but not this handler: discharge, treatment outcome
             * 3. Clinical information: dob (with related to age), gender, height, weight -> can
             * only be updated if patent does NOT have any treatment
             * 4. Other information can be updated freely
             */

            patient.FirstName = command.FirstName;
            patient.LastName = command.LastName;
            patient.PhoneNumber = command.PhoneNumber;
            patient.MedicalRecordCode = command.MedicalRecordCode;
            patient.HealthInsuranceCard = command.HealthInsuranceCard;
            patient.Address = command.Address;
            patient.City = command.City;
            patient.UpdatedAt = DateTimeOffset.UtcNow;
            if (patient.Treatments.Count == 0)
            {
                logger.LogDebug("Patient does not have any treatment, update patient clinical information");

                patient.DateOfBirth = command.DateOfBirth;
                patient.Gender = command.Gender;
                patient.Height = command.Height;
                patient.Weight = command.Weight;
            }

            // Save changes to database
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(ApplicationStatus.Updated);
        }
    }
}
