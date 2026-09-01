using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Respira.SagaAudit.Application.Features.Common;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Dtos;
using Wolverine;

namespace Respira.SagaAudit.Application.Features.UpdateDoctor.Commands
{
    /// <summary>
    /// Starts the UpdateDoctor saga slice by dispatching
    /// <see cref="UpdateDoctorByManagerCommand"/> to the saga. The saga id reported
    /// to the caller equals the entity id being updated.
    /// </summary>
    public class StartUpdateDoctorSagaCommandHandler(
        IMessageBus bus,
        ILogger<StartUpdateDoctorSagaCommandHandler> logger
    ) : ICommandHandler<StartUpdateDoctorSagaCommand, ApiResponse<StartSagaResult>>
    {
        public async Task<ApiResponse<StartSagaResult>> HandleAsync(
            StartUpdateDoctorSagaCommand command,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var sagaCommand = new UpdateDoctorByManagerCommand
                {
                    ManagerDoctorId = command.ManagerDoctorId,
                    EntityId = command.EntityId,
                    MediaId = command.MediaId,
                    Email = command.Email,
                    Phone = command.Phone,
                    Role = command.Role,
                    OldEmail = command.OldEmail,
                    OldPhone = command.OldPhone,
                    OldRole = command.OldRole,
                    FirstName = command.FirstName,
                    LastName = command.LastName,
                    Degrees = command.Degrees,
                    AcademicTitle = command.AcademicTitle,
                    Position = command.Position,
                    Gender = command.Gender,
                    CitizenIdentificationNumber = command.CitizenIdentificationNumber,
                    DateOfBirth = command.DateOfBirth,
                    Address = command.Address,
                    OldFirstName = command.OldFirstName,
                    OldLastName = command.OldLastName,
                    OldDegrees = command.OldDegrees,
                    OldAcademicTitle = command.OldAcademicTitle,
                    OldPosition = command.OldPosition,
                    OldGender = command.OldGender,
                    OldCitizenIdentificationNumber = command.OldCitizenIdentificationNumber,
                    OldDateOfBirth = command.OldDateOfBirth,
                    OldAddress = command.OldAddress,
                    HasNewMedia = command.HasNewMedia,
                    NewMediaId = command.NewMediaId,
                };

                await bus.SendAsync(sagaCommand);

                return ApiResponse<StartSagaResult>.Ok(
                    new StartSagaResult(command.EntityId),
                    "Saga started",
                    StatusCodes.Status202Accepted
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start UpdateDoctor saga for {EntityId}", command.EntityId);
                return ApiResponse<StartSagaResult>.Fail("Failed to start saga");
            }
        }
    }
}
