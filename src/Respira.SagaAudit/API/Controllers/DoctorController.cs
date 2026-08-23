using System.Security.Claims;
using System.Text.Json;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Respira.SagaAudit.API.Clients;
using Respira.SagaAudit.API.Dtos;
using Respira.SagaAudit.Application.Features.CreateDoctor.Commands;
using Respira.SagaAudit.Application.Features.DeleteDoctor.Commands;
using Respira.SagaAudit.Application.Features.UpdateDoctor.Commands;
using Respira.SagaAudit.Application.Services;
using Wolverine;

namespace Respira.SagaAudit.API.Controllers;

/// <summary>
/// Doctor lifecycle endpoints. Manager/Admin only. Each endpoint starts the
/// corresponding Wolverine saga (Create/Update/Delete doctor) which orchestrates
/// the cross-service workflow; the request is accepted (HTTP 202) and tracked
/// asynchronously via <see cref="ProcessTrackerService"/>.
/// </summary>
[ApiController]
[Route("api/v1/doctors")]
public class DoctorController(
    IMessageBus bus,
    MediaUploadClient mediaUpload,
    ProcessTrackerService trackerService
) : ControllerBase
{
    private bool IsManager() =>
        User.FindFirstValue(ClaimTypes.Role) is var role
        && (role == RoleType.Manager.ToString() || role == RoleType.Admin.ToString());

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
            return Forbid();
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest("Avatar file is required");
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
            return BadRequest("Invalid request payload");
        }

        if (dto is null)
        {
            return BadRequest("Invalid request payload");
        }

        var mediaId = await mediaUpload.UploadAsync(file, cancellationToken);

        var sagaId = Guid.NewGuid();
        await trackerService.CreateAsync(
            sagaId,
            "CreateDoctor",
            dto.ManagerDoctorId,
            null,
            cancellationToken
        );

        var command = new CreateDoctorByManagerCommand
        {
            SagaId = sagaId,
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
        };

        await bus.InvokeAsync(command, cancellationToken);
        return Accepted(new { sagaId });
    }

    /// <summary>Starts the UpdateDoctor saga (Auth -> Doctor -> Media). Manager/Admin only.</summary>
    [HttpPut]
    [Route("update")]
    public async Task<IActionResult> Update([FromBody] UpdateDoctorRequest request)
    {
        if (!IsManager())
        {
            return Forbid();
        }

        var command = new UpdateDoctorByManagerCommand
        {
            ManagerDoctorId = request.ManagerDoctorId,
            AuthUserId = request.AuthUserId,
            DoctorId = request.DoctorId,
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
        };

        await bus.SendAsync(command);
        return Accepted(new { sagaId = command.ManagerDoctorId });
    }

    /// <summary>Starts the DeleteDoctor saga (Media -> Doctor -> Auth). Manager/Admin only.</summary>
    [HttpDelete]
    [Route("delete")]
    public async Task<IActionResult> Delete([FromBody] DeleteDoctorRequest request)
    {
        if (!IsManager())
        {
            return Forbid();
        }

        var command = new DeleteDoctorByManagerCommand
        {
            ManagerDoctorId = request.ManagerDoctorId,
            AuthUserId = request.AuthUserId,
            DoctorId = request.DoctorId,
            MediaId = request.MediaId,
        };

        await bus.SendAsync(command);
        return Accepted(new { sagaId = command.ManagerDoctorId });
    }
}
