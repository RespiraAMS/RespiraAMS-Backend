using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Doctor.Application.Contracts.Data;
using Respira.Doctor.Application.Features.Doctors.Commands.Update.Events;
using Respira.Doctor.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Messages.Audit;
using Wolverine;
using DomainDoctor = Respira.Doctor.Domain.Entities.Doctor;

namespace Respira.Doctor.Application.Features.Doctors.Commands.Update
{
    public class UpdateDoctorProfileCommandHandler(
        IDoctorDbContext dbContext,
        IMessageBus bus,
        ILogger<UpdateDoctorProfileCommandHandler> logger
    ) : ICommandHandler<UpdateDoctorProfileCommand>
    {
        public async Task HandleAsync(
            UpdateDoctorProfileCommand command,
            CancellationToken cancellationToken = default
        )
        {
            BaseDoctor? profile = await dbContext.Doctors.FirstOrDefaultAsync(
                x => x.Id == command.DoctorId,
                cancellationToken
            );
            profile ??= await dbContext.HeadDepartmentDoctors.FirstOrDefaultAsync(
                x => x.Id == command.DoctorId,
                cancellationToken
            );

            if (profile is null)
            {
                logger.LogDebug(
                    "{SagaId} Doctor profile {DoctorId} not found for update",
                    command.SagaId,
                    command.DoctorId
                );

                await bus.PublishAsync(
                    new DoctorProfileUpdateFailure(command.SagaId, "Profile not found")
                );
                return;
            }

            if (profile.AccountId != command.AccountId)
            {
                await bus.PublishAsync(
                    new DoctorProfileUpdateFailure(command.SagaId, "Profile does not belong to the account")
                );
                return;
            }

            if (!await CanActorManageAsync(profile, command, cancellationToken))
            {
                await bus.PublishAsync(
                    new AuditEvent
                    {
                        ActorAccountId = command.ActorAccountId == Guid.Empty ? null : command.ActorAccountId,
                        ActorRole = command.ActorRole,
                        Action = "Authorization.ProfileUpdateDenied",
                        EntityType = profile is DomainDoctor ? "Doctor" : "HeadDepartmentDoctor",
                        EntityId = profile.Id,
                        CorrelationId = command.SagaId,
                    }
                );
                await bus.PublishAsync(
                    new DoctorProfileUpdateFailure(command.SagaId, "The actor cannot manage this profile")
                );
                return;
            }

            // The avatar that is about to be replaced; the saga deletes it once the
            // update is acknowledged. On a retry both ids are already equal, so the
            // cleanup is skipped and the flow stays idempotent.
            var oldMediaId = profile.MediaId ?? Guid.Empty;

            try
            {
                profile.FirstName = command.FirstName;
                profile.LastName = command.LastName;
                profile.Degrees = command.Degrees;
                profile.AcademicTitle = command.AcademicTitle;
                profile.Position = command.Position;
                profile.Gender = command.Gender;
                profile.CitizenIdentificationNumber = command.CitizenIdentificationNumber;
                profile.DateOfBirth = command.DateOfBirth;
                profile.Address = command.Address;

                if (command.NewMediaId != Guid.Empty)
                {
                    profile.MediaId = command.NewMediaId;
                }

                profile.UpdatedAt = DateTimeOffset.UtcNow;

                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "{SagaId} Failed to update doctor profile {DoctorId}",
                    command.SagaId,
                    command.DoctorId
                );

                await bus.PublishAsync(
                    new DoctorProfileUpdateFailure(command.SagaId, exception.Message)
                );
                return;
            }

            logger.LogInformation(
                "{SagaId} Updated doctor profile {DoctorId} (media {MediaId}, previous {OldMediaId})",
                command.SagaId,
                command.DoctorId,
                profile.MediaId,
                oldMediaId
            );

            await bus.PublishAsync(
                new DoctorProfileUpdated(
                    command.SagaId,
                    command.DoctorId,
                    profile.MediaId ?? Guid.Empty,
                    oldMediaId
                )
            );
            await bus.PublishAsync(
                new AuditEvent
                {
                    ActorAccountId = command.ActorAccountId == Guid.Empty ? null : command.ActorAccountId,
                    ActorRole = string.IsNullOrWhiteSpace(command.ActorRole) ? "System" : command.ActorRole,
                    Action = "Doctor.ProfileUpdated",
                    EntityType = profile is DomainDoctor ? "Doctor" : "HeadDepartmentDoctor",
                    EntityId = profile.Id,
                    CorrelationId = command.SagaId,
                    DetailsJson = System.Text.Json.JsonSerializer.Serialize(
                        new
                        {
                            ChangedFields = new[]
                            {
                                "FirstName",
                                "LastName",
                                "Degrees",
                                "AcademicTitle",
                                "Position",
                                "Gender",
                                "CitizenIdentificationNumber",
                                "DateOfBirth",
                                "Address",
                            },
                            MediaChanged = command.NewMediaId != Guid.Empty,
                        }
                    ),
                }
            );
        }

        private async Task<bool> CanActorManageAsync(
            BaseDoctor profile,
            UpdateDoctorProfileCommand command,
            CancellationToken cancellationToken
        )
        {
            if (command.ActorRole == "Admin")
            {
                return true;
            }

            if (
                profile.AccountId == command.ActorAccountId
                && command.ActorRole is "Doctor" or "HeadDepartmentDoctor"
            )
            {
                return true;
            }

            if (command.ActorRole != "HeadDepartmentDoctor" || profile is not DomainDoctor doctor)
            {
                return false;
            }

            var headId = await dbContext.HeadDepartmentDoctors
                .Where(item => item.AccountId == command.ActorAccountId)
                .Select(item => (Guid?)item.Id)
                .FirstOrDefaultAsync(cancellationToken);

            return headId.HasValue && doctor.HeadDepartmentDoctorId == headId.Value;
        }
    }
}
