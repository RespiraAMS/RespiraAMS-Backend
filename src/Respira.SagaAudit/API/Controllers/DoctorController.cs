using System.Security.Claims;
using System.Text.Json;
using Asp.Versioning;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Respira.SagaAudit.API.Clients;
using Respira.SagaAudit.API.Dtos;
using Respira.SagaAudit.Application.Features.Common;
using Respira.SagaAudit.Application.Features.CreateDoctor.Commands;
using Respira.SagaAudit.Application.Features.DeleteDoctor.Commands;
using Respira.SagaAudit.Application.Features.UpdateDoctor.Commands;
using Respira.ServiceDefaults.Dtos;
using Wolverine;

namespace Respira.SagaAudit.API.Controllers;

/// <summary>
/// Doctor lifecycle endpoints. Manager/Admin only. Each endpoint dispatches the
/// corresponding start-saga command (Create/Update/Delete doctor), which registers
/// the saga that orchestrates the cross-service workflow; the request is accepted
/// (HTTP 202) and tracked asynchronously.
/// </summary>
[ApiController]
[Route("api/{version:apiVersion}/sagas/doctors")]
[ApiVersion("1.0")]
public class DoctorController(
    IMessageBus bus,
    MediaUploadClient mediaUpload
) : ControllerBase
{
    private bool IsManager() =>
        User.FindFirstValue(ClaimTypes.Role) is var role
        && (role == nameof(RoleType.Manager) || role == nameof(RoleType.Admin));

    /// <summary>
    /// Starts the CreateDoctor saga (Auth -> Doctor -> Media). Manager/Admin only.
    /// Accepts multipart/form-data: the avatar as <c>file</c> and the metadata as a
    /// JSON string in the <c>request</c> field. The avatar is uploaded to the Media
    /// service first; the returned media id is then used to start the saga.
    /// </summary>
    [HttpPost]
    [Route("create")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create(
        [FromForm] IFormFile file,
        [FromForm] string request,
        CancellationToken cancellationToken
    )
    {
        if (!IsManager())
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                ApiResponse.Fail("You do not have permission to perform this action", StatusCodes.Status403Forbidden)
            );
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse.Fail("Avatar file is required", StatusCodes.Status400BadRequest));
        }

        CreateDoctorRequest? dto;
        try
        {
            dto = JsonSerializer.Deserialize<CreateDoctorRequest>(
                request,
                JsonSerializerOptions.Web
            );
        }
        catch (JsonException)
        {
            return BadRequest(ApiResponse.Fail("Invalid request payload", StatusCodes.Status400BadRequest));
        }

        if (dto is null)
        {
            return BadRequest(ApiResponse.Fail("Invalid request payload", StatusCodes.Status400BadRequest));
        }

        var mediaId = await mediaUpload.UploadAsync(file, cancellationToken);

        var result = await bus.InvokeAsync<ApiResponse<StartSagaResult>>(
            new StartCreateDoctorSagaCommand
            {
                ManagerDoctorId = dto.ManagerDoctorId,
                Email = dto.Email,
                Password = dto.Password,
                Phone = dto.Phone,
                Role = dto.Role,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Degrees = dto.Degrees,
                AcademicTitle = dto.AcademicTitle,
                Position = dto.Position,
                Gender = dto.Gender,
                CitizenIdentificationNumber = dto.CitizenIdentificationNumber,
                DateOfBirth = dto.DateOfBirth,
                Address = dto.Address,
                MediaId = mediaId,
            },
            cancellationToken
        );

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Starts the UpdateDoctor saga (Auth -> Doctor -> Media). Manager/Admin only.</summary>
    [HttpPut]
    [Route("update")]
    public async Task<IActionResult> Update([FromBody] UpdateDoctorRequest request)
    {
        if (!IsManager())
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                ApiResponse.Fail("You do not have permission to perform this action", StatusCodes.Status403Forbidden)
            );
        }

        var result = await bus.InvokeAsync<ApiResponse<StartSagaResult>>(
            new StartUpdateDoctorSagaCommand
            {
                ManagerDoctorId = request.ManagerDoctorId,
                EntityId = request.EntityId,
                MediaId = request.MediaId,
                Email = request.Email,
                Phone = request.Phone,
                Role = request.Role,
                OldEmail = request.OldEmail,
                OldPhone = request.OldPhone,
                OldRole = request.OldRole,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Degrees = request.Degrees,
                AcademicTitle = request.AcademicTitle,
                Position = request.Position,
                Gender = request.Gender,
                CitizenIdentificationNumber = request.CitizenIdentificationNumber,
                DateOfBirth = request.DateOfBirth,
                Address = request.Address,
                OldFirstName = request.OldFirstName,
                OldLastName = request.OldLastName,
                OldDegrees = request.OldDegrees,
                OldAcademicTitle = request.OldAcademicTitle,
                OldPosition = request.OldPosition,
                OldGender = request.OldGender,
                OldCitizenIdentificationNumber = request.OldCitizenIdentificationNumber,
                OldDateOfBirth = request.OldDateOfBirth,
                OldAddress = request.OldAddress,
                HasNewMedia = request.HasNewMedia,
                NewMediaId = request.NewMediaId,
            }
        );

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Starts the DeleteDoctor saga (Media -> Doctor -> Auth). Manager/Admin only.</summary>
    [HttpDelete]
    [Route("delete")]
    public async Task<IActionResult> Delete([FromBody] DeleteDoctorRequest request)
    {
        if (!IsManager())
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                ApiResponse.Fail("You do not have permission to perform this action", StatusCodes.Status403Forbidden)
            );
        }

        var result = await bus.InvokeAsync<ApiResponse<StartSagaResult>>(
            new StartDeleteDoctorSagaCommand
            {
                ManagerDoctorId = request.ManagerDoctorId,
                EntityId = request.EntityId,
                MediaId = request.MediaId,
            }
        );

        return StatusCode(result.StatusCode, result);
    }
}
