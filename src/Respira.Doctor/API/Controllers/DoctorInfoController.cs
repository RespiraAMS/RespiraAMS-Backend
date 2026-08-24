using Application.Features.Doctors.Get.Queries;
using Application.Features.Doctors.Get.Results;
using Microsoft.AspNetCore.Mvc;
using Respira.ServiceDefaults.Dtos;
using Wolverine;

namespace Respira.Doctor.API.Controllers;

/// <summary>
/// Admin endpoints for viewing doctor information across services.
/// </summary>
[ApiController]
[Route("api/v1/doctors")]
public class DoctorInfoController(IMessageBus messageBus) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<DoctorQueryResult>>> GetDoctorAsync(Guid id)
    {
        var result = await messageBus.InvokeAsync<ApiResponse<DoctorQueryResult>>(
            new DoctorQuery { Id = id }
        );
        return StatusCode(result.StatusCode, result);
    }
}
