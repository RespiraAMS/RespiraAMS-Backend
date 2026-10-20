using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Saga.API.Models;
using Respira.Saga.Application.Features.Update.Commands;
using Respira.Saga.Application.Features.Update.Queries;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Saga.API.Controllers
{
    /// <summary>
    /// Entry point of the update saga: one call updates the account, uploads the new
    /// avatar, updates the profile and deletes the avatar that was replaced. There is no
    /// compensation — a failed step is reported as failed and the request can be re-sent.
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/profile/update")]
    [Tags("Profile")]
    public class ProfileUpdateController(IMessageBus bus, ILogger<ProfileUpdateController> logger)
        : ControllerBase
    {
        /// <summary>Extensions an uploaded avatar is allowed to have.</summary>
        private static readonly string[] AllowedAvatarExtensions =
        [
            ".jpg",
            ".jpeg",
            ".png",
            ".webp",
        ];

        /// <summary>
        /// Starts a new profile update run. The request is multipart: a <c>request</c> text
        /// part carrying the new account and profile data as JSON and an <c>avatar</c> file part.
        /// </summary>
        /// <remarks>
        /// The endpoint returns as soon as the saga has been published, so the identifier in
        /// the response can be passed to the status endpoint to follow the progress.
        /// </remarks>
        /// <param name="request">Update payload as a JSON string.</param>
        /// <param name="avatar">New avatar image, stored by the Media microservice.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>The saga identifier, or a bad-request failure describing what was missing.</returns>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Start(
            [FromForm]
            string request,
            [FromForm]
                IFormFile? avatar,
            CancellationToken cancellationToken
        )
        {
            if (string.IsNullOrWhiteSpace(request))
            {
                return Result<Guid>
                    .Failure(new Error(ApplicationStatus.BadRequest, "A 'request' part is required"))
                    .ToApiResponse();
            }

            UpdateProfileRequest? payload;

            try
            {
                payload = JsonSerializer.Deserialize<UpdateProfileRequest>(
                    request,
                    UpdateProfileRequest.SerializerOptions
                );
            }
            catch (JsonException exception)
            {
                return Result<Guid>
                    .Failure(
                        new Error(
                            ApplicationStatus.BadRequest,
                            $"The 'request' part is not valid JSON: {exception.Message}"
                        )
                    )
                    .ToApiResponse();
            }

            if (payload is null)
            {
                return Result<Guid>
                    .Failure(
                        new Error(ApplicationStatus.BadRequest, "The 'request' part is empty")
                    )
                    .ToApiResponse();
            }

            var invalidField = Validate(payload);

            if (invalidField is not null)
            {
                return Result<Guid>
                    .Failure(new Error(ApplicationStatus.BadRequest, invalidField))
                    .ToApiResponse();
            }

            var sagaId = Guid.CreateVersion7();
            string? tempAvatarPath = null;
            if (avatar is not null && avatar.Length > 0)
            {
                var extension = Path.GetExtension(avatar.FileName).ToLowerInvariant();

                if (!AllowedAvatarExtensions.Contains(extension))
                {
                    return Result<Guid>
                        .Failure(
                            new Error(
                                ApplicationStatus.BadRequest,
                                $"The avatar must be one of: {string.Join(", ", AllowedAvatarExtensions)}"
                            )
                        )
                        .ToApiResponse();
                }

                tempAvatarPath = await StoreAvatarAsync(
                    avatar,
                    sagaId,
                    extension,
                    cancellationToken
                );
            }

            var command = CreateCommand(payload, sagaId, tempAvatarPath);

            await bus.PublishAsync(command);

            logger.LogInformation(
                "Profile update {SagaId} started for account {AccountId} / profile {DoctorId}",
                sagaId,
                command.AccountId,
                command.DoctorId
            );

            return Result<Guid>.Success(ApplicationStatus.Created, sagaId).ToApiResponse();
        }

        private static StartUpdateCommand CreateCommand(
            UpdateProfileRequest payload,
            Guid sagaId,
            string? tempAvatarPath
        ) => new()
        {
            SagaId = sagaId,
            AccountId = payload.AccountId,
            DoctorId = payload.DoctorId,
            Email = payload.Email,
            Phone = payload.Phone,
            Password = string.IsNullOrWhiteSpace(payload.Password) ? null : payload.Password,
            FirstName = payload.FirstName,
            LastName = payload.LastName,
            Degrees = payload.Degrees,
            AcademicTitle = payload.AcademicTitle,
            Position = payload.Position,
            Gender = payload.Gender,
            CitizenIdentificationNumber = payload.CitizenIdentificationNumber,
            DateOfBirth = payload.DateOfBirth,
            Address = payload.Address,
            TempAvatarPath = tempAvatarPath,
        };

        /// <summary>
        /// Reports where a profile update run currently is. Completed runs are removed
        /// from the saga storage, so a not-found means the flow already finished
        /// (successfully or not) or never existed.
        /// </summary>
        /// <param name="sagaId">Identifier returned by the start endpoint.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>The live saga state, or a resource-not-found failure.</returns>
        [HttpGet("{sagaId:guid}")]
        [ProducesResponseType(typeof(Result<UpdateStatusResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<UpdateStatusResult>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStatus(
            Guid sagaId,
            CancellationToken cancellationToken
        )
        {
            var result = await bus.InvokeAsync<Result<UpdateStatusResult>>(
                new GetUpdateStatusQuery { SagaId = sagaId },
                cancellationToken
            );

            return result.ToApiResponse();
        }

        /// <summary>Returns the first field that fails validation, or null when valid.</summary>
        private static string? Validate(UpdateProfileRequest payload)
        {
            if (payload.AccountId == Guid.Empty)
            {
                return "Account id is required";
            }

            if (payload.DoctorId == Guid.Empty)
            {
                return "Profile id is required";
            }

            if (string.IsNullOrWhiteSpace(payload.Email))
            {
                return "Email is required";
            }

            if (string.IsNullOrWhiteSpace(payload.Phone))
            {
                return "Phone is required";
            }

            if (string.IsNullOrWhiteSpace(payload.FirstName))
            {
                return "First name is required";
            }

            if (string.IsNullOrWhiteSpace(payload.LastName))
            {
                return "Last name is required";
            }

            if (string.IsNullOrWhiteSpace(payload.CitizenIdentificationNumber))
            {
                return "Citizen identification number is required";
            }

            if (string.IsNullOrWhiteSpace(payload.Address))
            {
                return "Address is required";
            }

            if (!Enum.IsDefined(payload.Gender))
            {
                return $"Unknown gender '{payload.Gender}'";
            }

            if (!Enum.IsDefined(payload.Position))
            {
                return $"Unknown position '{payload.Position}'";
            }

            if (!Enum.IsDefined(payload.AcademicTitle))
            {
                return $"Unknown academic title '{payload.AcademicTitle}'";
            }

            return null;
        }

        /// <summary>
        /// Writes the avatar to a temp file the saga consumes at the media step. The file
        /// lives outside the message so it never has to be serialized into the broker.
        /// </summary>
        private static async Task<string> StoreAvatarAsync(
            IFormFile avatar,
            Guid sagaId,
            string extension,
            CancellationToken cancellationToken
        )
        {
            var directory = Path.Combine(Path.GetTempPath(), "respira-profile-update");

            Directory.CreateDirectory(directory);

            var path = Path.Combine(directory, $"{sagaId}{extension}");

            await using var stream = System.IO.File.Create(path);
            await avatar.CopyToAsync(stream, cancellationToken);

            return path;
        }
    }
}
