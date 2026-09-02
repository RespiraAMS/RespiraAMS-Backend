using Application.Features.Doctors.Get.Queries;
using Application.Features.Doctors.Get.Results;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Doctor.API.Dtos;
using Respira.ServiceDefaults.Dtos;
using Wolverine;

namespace Respira.Doctor.API.Controllers;

/// <summary>
/// Admin endpoints for viewing doctor information across services.
/// </summary>
[ApiController]
[Route("api/{version:apiVersion}/doctors")]
[ApiVersion("1.0")]
public class DoctorInfoController(IMessageBus messageBus) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<Result<DoctorQueryResult>>> GetDoctorAsync(Guid id)
    {
        var result = await messageBus.InvokeAsync<Result<DoctorQueryResult>>(
            new DoctorQuery { Id = id }
        );
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet]
    [ProducesResponseType<Result<Pagination<DoctorListItemResult>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<Result<Pagination<DoctorListItemResult>>>> GetDoctorsAsync(
        [FromQuery] GetDoctorsRequestDto request
    )
    {
        var result = await messageBus.InvokeAsync<Pagination<DoctorListItemResult>>(
            request.ToQuery()
        );
        return Ok(Result<Pagination<DoctorListItemResult>>.Ok(result));
    }
}
