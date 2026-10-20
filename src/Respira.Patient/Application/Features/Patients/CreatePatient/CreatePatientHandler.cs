using Microsoft.Extensions.Logging;
using Respira.Patient.Application.Contracts.Data;
using Respira.Patient.Application.Contracts.Mappers;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;
using Microsoft.EntityFrameworkCore;

namespace Respira.Patient.Application.Features.Patients.CreatePatient
{
    public class CreatePatientHandler(
        IDbContext context,
        ICreateMapper<CreatePatientCommand, Domain.Entities.Patient> mapper,
        ILogger<CreatePatientHandler> logger)
        : ICommandHandler<CreatePatientCommand, Result<CreatePatientResult>>
    {
        public async Task<Result<CreatePatientResult>> HandleAsync(CreatePatientCommand command, CancellationToken cancellationToken = default)
        {
            // Business rule: medical record code must be unique
            if (await context.Patients.AnyAsync(x => x.MedicalRecordCode == command.MedicalRecordCode, cancellationToken))
            {
                logger.LogDebug("Medical record code {code} is already existed", command.MedicalRecordCode);
                return Result<CreatePatientResult>.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Medical record code is already existed"));
            }

            // Map from command to model
            var mapResult = mapper.ToModel(command);
            if (mapResult.IsFailure())
            {
                logger.LogDebug("Failed to map from command to model: {error}", mapResult.Error);
                return Result<CreatePatientResult>.Failure(mapResult.Error!);
            }
            var patient = mapResult.Data!;

            // Business rule: only accept patient above 15 years old. CAP in children have different
            // treatment rules, and is not covered by this system
            if (!patient.IsValidAge())
            {
                logger.LogDebug("Patient is under 15 years old, cannot be created");
                return Result<CreatePatientResult>.Failure(new ApplicationError(ApplicationStatus.BusinessRuleViolation, "Patient is under 15 years old, cannot be created"));
            }

            // Save changes to database
            await context.Patients.AddAsync(patient, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result<CreatePatientResult>.Success(ApplicationStatus.Created, new CreatePatientResult(patient.Id));
        }
    }
}
