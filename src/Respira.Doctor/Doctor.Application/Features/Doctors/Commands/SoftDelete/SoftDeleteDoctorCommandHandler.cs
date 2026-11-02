using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Doctor.Application.Contracts.Data;
using Respira.Doctor.Application.Features.Doctors.Commands.SoftDelete.Events;
using Respira.Doctor.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Messages.Audit;
using Wolverine;
using DomainDoctor = Respira.Doctor.Domain.Entities.Doctor;

namespace Respira.Doctor.Application.Features.Doctors.Commands.SoftDelete
{
    public class SoftDeleteDoctorCommandHandler(
        IDoctorDbContext dbContext,
        IMessageBus bus,
        ILogger<SoftDeleteDoctorCommandHandler> logger
    ) : ICommandHandler<SoftDeleteDoctorCommand>
    {
        public async Task HandleAsync(
            SoftDeleteDoctorCommand command,
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
                // Compensation must be idempotent: "already gone" counts as success.
                logger.LogDebug(
                    "{SagaId} Doctor profile {DoctorId} already gone, treating soft delete as completed",
                    command.SagaId,
                    command.DoctorId
                );

                await bus.PublishAsync(
                    new SoftDeleteDoctorCompleted(command.SagaId, command.DoctorId)
                );
                return;
            }

            if (
                !string.IsNullOrWhiteSpace(command.ActorRole)
                && !await CanActorManageAsync(profile, command, cancellationToken)
            )
            {
                await bus.PublishAsync(
                    new AuditEvent
                    {
                        ActorAccountId = command.ActorAccountId == Guid.Empty ? null : command.ActorAccountId,
                        ActorRole = command.ActorRole,
                        Action = "Authorization.ProfileDeleteDenied",
                        EntityType = profile is DomainDoctor ? "Doctor" : "HeadDepartmentDoctor",
                        EntityId = profile.Id,
                        CorrelationId = command.SagaId,
                    }
                );
                await bus.PublishAsync(
                    new SoftDeleteDoctorFailure(command.SagaId, "The actor cannot manage this profile")
                );
                return;
            }

            try
            {
                profile.IsDeleted = true;
                profile.DeletedAt = DateTimeOffset.UtcNow;

                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "{SagaId} Failed to soft delete doctor profile {DoctorId}",
                    command.SagaId,
                    command.DoctorId
                );

                await bus.PublishAsync(
                    new SoftDeleteDoctorFailure(command.SagaId, exception.Message)
                );
                return;
            }

            logger.LogInformation(
                "{SagaId} Soft deleted doctor profile {DoctorId}",
                command.SagaId,
                command.DoctorId
            );

            await bus.PublishAsync(
                new SoftDeleteDoctorCompleted(command.SagaId, command.DoctorId)
            );
            await bus.PublishAsync(
                new AuditEvent
                {
                    ActorAccountId = command.ActorAccountId == Guid.Empty ? null : command.ActorAccountId,
                    ActorRole = string.IsNullOrWhiteSpace(command.ActorRole) ? "System" : command.ActorRole,
                    Action = "Doctor.ProfileSoftDeleted",
                    EntityType = profile is DomainDoctor ? "Doctor" : "HeadDepartmentDoctor",
                    EntityId = profile.Id,
                    CorrelationId = command.SagaId,
                }
            );
        }

        private async Task<bool> CanActorManageAsync(
            BaseDoctor profile,
            SoftDeleteDoctorCommand command,
            CancellationToken cancellationToken
        )
        {
            if (profile.AccountId != command.TargetAccountId)
            {
                return false;
            }

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
