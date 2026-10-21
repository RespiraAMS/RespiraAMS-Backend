using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Patient.Application.Contracts.Data;
using Respira.Patient.Application.Contracts.Mappers;
using Respira.Patient.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Features.Patients.AddNextOfKin
{
    public class AddNextOfKinHandler(
        IDbContext context,
        ICreateMapper<AddNextOfKinCommand, NextOfKin> mapper,
        ILogger<AddNextOfKinHandler> logger)
        : ICommandHandler<AddNextOfKinCommand, Result<AddNextOfKinResult>>
    {
        public async Task<Result<AddNextOfKinResult>> HandleAsync(AddNextOfKinCommand command, CancellationToken cancellationToken = default)
        {
            // Check if patient exists
            var patient = await context.Patients
                .Include(x => x.NextOfKins)
                .FirstOrDefaultAsync(x => x.Id == command.PatientId, cancellationToken);
            if (patient is null)
            {
                logger.LogDebug("Patient not found: {Id}", command.PatientId);
                return Result<AddNextOfKinResult>.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Patient not found"));
            }

            // Check for unique. The only thing that can be unique is the phone number,
            // while relationship is not unique (sibling for example, you can have many),
            // and full name is not really safe to judge (there is no rule state that your
            // family member must have a different than you, though it's really unsual).
            // And since phone number is what doctor truly care (for contact when they need
            // consent, or notify), so phone number check is enough
            // NOTE: unique check must be of a same patient. A same person can be next of kin
            // of multuple patients
            if (patient.NextOfKins.Any(x => x.PhoneNumber == command.PhoneNumber))
            {
                logger.LogDebug("Next of kin phone number already registered for this patient: {detail}", new
                {
                    command.PatientId,
                    command.PhoneNumber
                });
                return Result<AddNextOfKinResult>.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Next of kin phone number already registered for this patient"));
            }

            // Map command to domain model
            var mapResult = mapper.ToModel(command);
            if (mapResult.IsFailure())
            {
                logger.LogDebug("Failed to map command to domain model: {ApplicationError}", mapResult.Error!);
                return Result<AddNextOfKinResult>.Failure(mapResult.Error!);
            }
            var nextOfKin = mapResult.Data!;

            // Save changes to database
            await context.NextOfKins.AddAsync(nextOfKin, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result<AddNextOfKinResult>.Success(ApplicationStatus.Created, new AddNextOfKinResult(nextOfKin.Id));
        }
    }
}
