using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Saga.API.Models;
using Respira.Saga.Application.Features.Delete.Commands;
using Respira.Saga.Application.Features.Delete.Queries;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Saga.API.Controllers
{
    /// <summary>
    /// Entry point of the delete saga: one call soft deletes the profile, soft deletes the
    /// account and deletes the avatar's media asset. There is no compensation — every step
    /// treats "already gone" as success, so a failed run can simply be re-sent.
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/profile/delete")]
    [Tags("Profile")]
    public class ProfileDeleteController(IMessageBus bus, ILogger<ProfileDeleteController> logger)
        : ControllerBase
    {
        /// <summary>
        /// Starts a new profile deletion run.
        /// </summary>
        /// <remarks>
        /// The endpoint returns as soon as the saga has been published, so the identifier in
        /// the response can be passed to the status endpoint to follow the progress.
        /// </remarks>
        /// <param name="payload">Identifiers of the account, profile and avatar to remove.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>The saga identifier, or a bad-request failure describing what was missing.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Start(
            [FromBody] DeleteProfileRequest payload,
            CancellationToken cancellationToken
        )
        {
            if (payload is null)
            {
                return Result<Guid>
                    .Failure(new Error(ApplicationStatus.BadRequest, "A request body is required"))
                    .ToApiResponse();
            }

            if (payload.AccountId == Guid.Empty)
            {
                return Result<Guid>
                    .Failure(new Error(ApplicationStatus.BadRequest, "Account id is required"))
                    .ToApiResponse();
            }

            if (payload.DoctorId == Guid.Empty)
            {
                return Result<Guid>
                    .Failure(new Error(ApplicationStatus.BadRequest, "Profile id is required"))
                    .ToApiResponse();
            }

            var sagaId = Guid.CreateVersion7();

            var command = new StartDeleteCommand
            {
                SagaId = sagaId,
                AccountId = payload.AccountId,
                DoctorId = payload.DoctorId,
                MediaId = payload.MediaId,
            };

            await bus.PublishAsync(command);

            logger.LogInformation(
                "Profile deletion {SagaId} started for account {AccountId} / profile {DoctorId}",
                sagaId,
                command.AccountId,
                command.DoctorId
            );

            return Result<Guid>.Success(ApplicationStatus.Created, sagaId).ToApiResponse();
        }

        /// <summary>
        /// Reports where a profile deletion run currently is. Completed runs are removed
        /// from the saga storage, so a not-found means the flow already finished
        /// (successfully or not) or never existed.
        /// </summary>
        /// <param name="sagaId">Identifier returned by the start endpoint.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>The live saga state, or a resource-not-found failure.</returns>
        [HttpGet("{sagaId:guid}")]
        [ProducesResponseType(typeof(Result<DeleteStatusResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<DeleteStatusResult>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStatus(
            Guid sagaId,
            CancellationToken cancellationToken
        )
        {
            var result = await bus.InvokeAsync<Result<DeleteStatusResult>>(
                new GetDeleteStatusQuery { SagaId = sagaId },
                cancellationToken
            );

            return result.ToApiResponse();
        }
    }
}
