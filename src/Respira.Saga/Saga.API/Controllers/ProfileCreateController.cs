using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Saga.API.Models;
using Respira.Saga.Application.Features.Create.Commands;
using Respira.Saga.Application.Features.Create.Queries;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Saga.API.Controllers
{
    /// <summary>
    /// Entry point of the profile creation saga: one call creates the account, the profile, the
    /// avatar media asset and sends the verification email, with compensation if a step
    /// fails.
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/profile/create")]
    [Tags("Profile")]
    public class ProfileCreateController(IMessageBus bus, ILogger<ProfileCreateController> logger)
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
        /// Starts a new profile creation run. The request is multipart: a <c>request</c> text part
        /// carrying the profile as JSON and an <c>avatar</c> file part.
        /// </summary>
        /// <remarks>
        /// The endpoint returns as soon as the saga has been published, so the identifier in
        /// the response can be passed to the status endpoint to follow the progress.
        /// </remarks>
        /// <param name="request">Profile payload as a JSON string.</param>
        /// <param name="avatar">Avatar image, stored by the Media microservice.</param>
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

            CreateProfileRequest? payload;

            try
            {
                payload = JsonSerializer.Deserialize<CreateProfileRequest>(
                    request,
                    CreateProfileRequest.SerializerOptions
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

            if (avatar is null || avatar.Length == 0)
            {
                return Result<Guid>
                    .Failure(new Error(ApplicationStatus.BadRequest, "An 'avatar' file is required"))
                    .ToApiResponse();
            }

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

            var sagaId = Guid.CreateVersion7();
            var tempAvatarPath = await StoreAvatarAsync(avatar, sagaId, extension, cancellationToken);

            var command = new StartCreateCommand
            {
                SagaId = sagaId,
                Email = payload.Email,
                Password = payload.Password,
                Phone = payload.Phone,
                Role = payload.Role,
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

            await bus.PublishAsync(command);

            logger.LogInformation(
                "Create {SagaId} started for {Email} ({Role})",
                sagaId,
                command.Email,
                command.Role
            );

            return Result<Guid>.Success(ApplicationStatus.Created, sagaId).ToApiResponse();
        }

        /// <summary>
        /// Reports where a profile creation run currently is. Completed runs are removed from
        /// the saga storage, so a not-found means the flow already finished (successfully
        /// or not) or never existed.
        /// </summary>
        /// <param name="sagaId">Identifier returned by the start endpoint.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>The live saga state, or a resource-not-found failure.</returns>
        [HttpGet("{sagaId:guid}")]
        [ProducesResponseType(typeof(Result<CreateStatusResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<CreateStatusResult>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStatus(
            Guid sagaId,
            CancellationToken cancellationToken
        )
        {
            var result = await bus.InvokeAsync<Result<CreateStatusResult>>(
                new GetCreateStatusQuery { SagaId = sagaId },
                cancellationToken
            );

            return result.ToApiResponse();
        }

        /// <summary>Returns the first field that fails validation, or null when valid.</summary>
        private static string? Validate(CreateProfileRequest payload)
        {
            if (string.IsNullOrWhiteSpace(payload.Email))
            {
                return "Email is required";
            }

            if (string.IsNullOrWhiteSpace(payload.Password))
            {
                return "Password is required";
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

            if (!Enum.IsDefined(payload.Role))
            {
                return $"Unknown role '{payload.Role}'";
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
            var directory = Path.Combine(Path.GetTempPath(), "respira-profile-create");

            Directory.CreateDirectory(directory);

            var path = Path.Combine(directory, $"{sagaId}{extension}");

            await using var stream = System.IO.File.Create(path);
            await avatar.CopyToAsync(stream, cancellationToken);

            return path;
        }
    }
}
