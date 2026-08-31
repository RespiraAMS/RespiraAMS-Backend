using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Respira.SagaAudit.Application.Features.Common;
using Respira.SagaAudit.Application.Services;
using Respira.ServiceDefaults.Constracts.CQRS;
using Respira.ServiceDefaults.Dtos;
using Wolverine;

namespace Respira.SagaAudit.Application.Features.CreateDoctor.Commands
{
    /// <summary>
    /// Starts the CreateDoctor saga slice: registers the process tracker and
    /// dispatches <see cref="CreateDoctorByManagerCommand"/> to the saga.
    /// </summary>
    public class StartCreateDoctorSagaCommandHandler(
        IMessageBus bus,
        ProcessTrackerService trackerService,
        ILogger<StartCreateDoctorSagaCommandHandler> logger
    ) : ICommandHandler<StartCreateDoctorSagaCommand, ApiResponse<StartSagaResult>>
    {
        public async Task<ApiResponse<StartSagaResult>> HandleAsync(
            StartCreateDoctorSagaCommand command,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var sagaId = Guid.NewGuid();
                await trackerService.CreateAsync(
                    sagaId,
                    "CreateDoctor",
                    command.ManagerDoctorId,
                    null,
                    cancellationToken
                );

                var sagaCommand = new CreateDoctorByManagerCommand
                {
                    SagaId = sagaId,
                    ManagerDoctorId = command.ManagerDoctorId,
                    Email = command.Email,
                    Password = command.Password,
                    Phone = command.Phone,
                    Role = command.Role,
                    FirstName = command.FirstName,
                    LastName = command.LastName,
                    Degrees = command.Degrees,
                    AcademicTitle = command.AcademicTitle,
                    Position = command.Position,
                    Gender = command.Gender,
                    CitizenIdentificationNumber = command.CitizenIdentificationNumber,
                    DateOfBirth = command.DateOfBirth,
                    Address = command.Address,
                    MediaId = command.MediaId,
                };

                await bus.InvokeAsync(sagaCommand, cancellationToken);

                return ApiResponse<StartSagaResult>.Ok(
                    new StartSagaResult(sagaId),
                    "Saga started",
                    StatusCodes.Status202Accepted
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start CreateDoctor saga");
                return ApiResponse<StartSagaResult>.Fail("Failed to start saga");
            }
        }
    }
}
