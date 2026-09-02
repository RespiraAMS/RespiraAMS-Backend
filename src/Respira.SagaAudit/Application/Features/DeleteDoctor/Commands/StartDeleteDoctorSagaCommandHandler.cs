using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Respira.SagaAudit.Application.Features.Common;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Dtos;
using Wolverine;

namespace Respira.SagaAudit.Application.Features.DeleteDoctor.Commands
{
    /// <summary>
    /// Starts the DeleteDoctor saga slice by dispatching
    /// <see cref="DeleteDoctorByManagerCommand"/> to the saga. The saga id reported
    /// to the caller equals the entity id being deleted.
    /// </summary>
    public class StartDeleteDoctorSagaCommandHandler(
        IMessageBus bus,
        ILogger<StartDeleteDoctorSagaCommandHandler> logger
    ) : ICommandHandler<StartDeleteDoctorSagaCommand, Result<StartSagaResult>>
    {
        public async Task<Result<StartSagaResult>> HandleAsync(
            StartDeleteDoctorSagaCommand command,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var sagaCommand = new DeleteDoctorByManagerCommand
                {
                    ManagerDoctorId = command.ManagerDoctorId,
                    EntityId = command.EntityId,
                    MediaId = command.MediaId,
                };

                await bus.SendAsync(sagaCommand);

                return Result<StartSagaResult>.Ok(
                    new StartSagaResult(command.EntityId),
                    "Saga started",
                    StatusCodes.Status202Accepted
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start DeleteDoctor saga for {EntityId}", command.EntityId);
                return Result<StartSagaResult>.Fail("Failed to start saga");
            }
        }
    }
}
