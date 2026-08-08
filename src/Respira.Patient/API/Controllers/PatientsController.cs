using Application.Features.Patients.CreatePatient;
using Application.Features.Patients.DeletePatient;
using Application.Features.Patients.GetPagedPatient;
using Application.Features.Patients.GetPatientById;
using Microsoft.AspNetCore.Mvc;
using Respira.Patient.API.Dtos;
using Respira.ServiceDefaults.Dtos;
using Wolverine;

namespace Respira.Patient.API.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/patients")]
public class PatientsController(IMessageBus bus) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ApiResponse<CreatePatientResult>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePatient([FromBody] CreatePatientCommand req)
    {
        var result = await bus.InvokeAsync<CreatePatientResult>(req);
        var resp = ApiResponse<CreatePatientResult>.Ok(result, statusCode: StatusCodes.Status201Created);
        return CreatedAtAction(nameof(GetPatient), new { id = result.Id }, resp);
    }

    [HttpGet]
    [Route("{id:guid}")]
    [ProducesResponseType<ApiResponse<PatientResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPatient(Guid id)
    {
        var result = await bus.InvokeAsync<PatientResult>(new GetPatientByIdQuery { Id = id });
        var resp = ApiResponse<PatientResult>.Ok(result);
        return Ok(resp);
    }

    [HttpGet]
    [ProducesResponseType<ApiResponse<Pagination<PagedPatientItem>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPatients([FromQuery] GetPagedPatientRequestDto req)
    {
        var result = await bus.InvokeAsync<Pagination<PagedPatientItem>>(req.ToQuery());
        var resp = ApiResponse<Pagination<PagedPatientItem>>.Ok(result);
        return Ok(resp);
    }

    /// <remarks>
    /// If a patient already receive treatment, then <code>dateOfBirth</code>
    /// and <code>isMale</code> won't be updated (others information still update normally).
    /// If patient hasn't received any treatment, then all information can be updated normally
    /// </remarks>
    [HttpPut]
    [Route("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdatePatient(Guid id, [FromBody] UpdatePatientRequestDto req)
    {
        await bus.InvokeAsync(req.ToCommand(id));
        return NoContent();
    }

    /// <remarks>
    /// Only patient that has received atleast 1 treatment can be discharge 
    /// </remarks>
    [HttpPut]
    [Route("{id:guid}/discharge")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DischargePatient(Guid id, [FromBody] DischargePatientRequestDto req)
    {
        await bus.InvokeAsync(req.ToCommand(id));
        return NoContent();
    }

    /// <remarks>
    /// Only patient that hasn't received any treatment can be deleted
    /// </remarks>
    [HttpDelete]
    [Route("{id:guid}")]
    public async Task<IActionResult> DeletePatient(Guid id)
    {
        await bus.InvokeAsync(new DeletePatientCommand { Id = id });
        return NoContent();
    }
}