using System.Security.Claims;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Respira.SagaAudit.API.Dtos;
using Respira.SagaAudit.Application.Features.CreateDoctor.Commands;
using Respira.SagaAudit.Application.Features.DeleteDoctor.Commands;
using Respira.SagaAudit.Application.Features.UpdateDoctor.Commands;
using Wolverine;

namespace Respira.SagaAudit.API.Controllers;

[ApiController]
[Route("api/v1/doctors")]
public class DoctorController(IMessageBus bus) : ControllerBase
{
    private bool IsManager() =>
        User.FindFirstValue(ClaimTypes.Role) is var role
        && (role == RoleType.Manager.ToString() || role == RoleType.Admin.ToString());

    /// <summary>Starts the CreateDoctor saga (Auth -> Doctor -> Media). Manager/Admin only.</summary>
    [HttpPost]
    [Route("create")]
    public async Task<IActionResult> Create([FromBody] CreateDoctorRequest request)
    {
        if (!IsManager())
        {
            return Forbid();
        }

        var command = new CreateDoctorByManagerCommand
        {
            ManagerDoctorId = request.ManagerDoctorId,
            Email = request.Email,
            Password = request.Password,
            Phone = request.Phone,
            Role = request.Role,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Degrees = request.Degrees,
            AcademicTitle = request.AcademicTitle,
            Position = request.Position,
            Gender = request.Gender,
            CitizenIdentificationNumber = request.CitizenIdentificationNumber,
            DateOfBirth = request.DateOfBirth,
            Address = request.Address,
            MediaFileName = request.MediaFileName,
            MediaContentType = request.MediaContentType,
            MediaSize = request.MediaSize,
            MediaData = request.MediaData,
        };

        await bus.SendAsync(command);
        return Accepted(new { sagaId = command.ManagerDoctorId });
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
            NewMediaFileName = request.NewMediaFileName,
            NewMediaContentType = request.NewMediaContentType,
            NewMediaSize = request.NewMediaSize,
            NewMediaData = request.NewMediaData,
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
