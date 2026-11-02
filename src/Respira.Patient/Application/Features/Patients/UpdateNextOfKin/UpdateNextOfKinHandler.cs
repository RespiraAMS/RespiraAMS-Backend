using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Patient.Application.Contracts.Data;
using Respira.Patient.Application.Contracts.Mappers;
using Respira.Patient.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Features.Patients.UpdateNextOfKin
{
    public class UpdateNextOfKinHandler(
        IDbContext context,
        IUpdateMapper<NextOfKin, UpdateNextOfKinCommand> mapper,
        ILogger<UpdateNextOfKinHandler> logger)
        : ICommandHandler<UpdateNextOfKinCommand, Result>
    {
        public async Task<Result> HandleAsync(UpdateNextOfKinCommand command, CancellationToken cancellationToken = default)
        {
            // Get next of kin by ID
            var nextOfKin = await context.NextOfKins.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
            if (nextOfKin is null)
            {
                logger.LogDebug("Next of kin not found: {Id}", command.Id);
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Next of kin not found"));
            }

            // Check of phone number uniqueness
            var phoneExist = await context.NextOfKins
                .AnyAsync(x => x.Id != command.Id && x.PatientId == nextOfKin.PatientId && x.PhoneNumber == command.PhoneNumber, cancellationToken);
            if (phoneExist)
            {
                logger.LogDebug("Next of kin phone number already registered for this patient: {detail}", new
                {
                    command.Id,
                    nextOfKin.PatientId,
                    command.PhoneNumber
                });
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Next of kin phone number already registered for this patient"));
            }

            // Map command to domain model
            var mapResult = mapper.MapModel(nextOfKin, command);
            if (mapResult.IsFailure())
            {
                logger.LogDebug("Failed to map command to domain model: {ApplicationError}", mapResult.Error!);
                return Result.Failure(mapResult.Error!);
            }

            // Save changes to database
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(ApplicationStatus.Updated);
        }
    }
}
