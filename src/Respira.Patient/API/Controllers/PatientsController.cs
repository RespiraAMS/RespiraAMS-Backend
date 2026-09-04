using Application.Features.Patients.CreatePatient;
using Application.Features.Patients.DeletePatient;
using Application.Features.Patients.GetPagedPatient;
using Application.Features.Patients.GetPatientById;
using Application.Features.Treatments.CreateTreatment;
using Application.Features.Treatments.GetTreatmentById;
using Microsoft.AspNetCore.Mvc;
using Respira.Patient.API.Dtos;
using Respira.ServiceDefaults.Contracts.Results;
using Respira.ServiceDefaults.Dtos;
using Wolverine;

namespace Respira.Patient.API.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/patients")]
public class PatientsController(IMessageBus bus, ILogger<PatientsController> logger) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<Result<CreatePatientResult>>(StatusCodes.Status201Created)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePatient([FromBody] CreatePatientCommand req)
    {
        var result = await bus.InvokeAsync<Result<CreateTreatmentResult>>(req);
        return result.ToApiResponse();
    }

    [HttpGet]
    [Route("{id:guid}")]
    [ProducesResponseType<Result<PatientResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPatient(Guid id)
    {
        var result = await bus.InvokeAsync<Result<PatientResult>>(new GetPatientByIdQuery(id));
        return result.ToApiResponse();
    }

    [HttpGet]
    [ProducesResponseType<Result<Pagination<PagedPatientItem>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPatients([FromQuery] GetPagedPatientRequestDto req)
    {
        var result = await bus.InvokeAsync<Result<Pagination<PagedPatientItem>>>(req.ToQuery());
        return result.ToApiResponse();
    }

    /// <summary>
    /// UpdatePatient
    /// </summary>
    /// <remarks>
    /// If a patient already receive treatment, then <c>dateOfBirth</c>
    /// and <c>isMale</c> won't be updated (others information still update normally).
    /// If patient hasn't received any treatment, then all information can be updated normally
    /// </remarks>
    [HttpPut]
    [Route("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdatePatient(Guid id, [FromBody] UpdatePatientRequestDto req)
    {
        var result = await bus.InvokeAsync<Result>(req.ToCommand(id));
        return result.ToApiResponse();
    }

    /// <summary>
    /// DischargePatient
    /// </summary>
    /// <remarks>
    /// Only patient that has received atleast 1 treatment can be discharge
    /// </remarks>
    [HttpPut]
    [Route("{id:guid}/discharge")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DischargePatient(Guid id, [FromBody] DischargePatientRequestDto req)
    {
        var result = await bus.InvokeAsync<Result>(req.ToCommand(id));
        return result.ToApiResponse();
    }

    /// <summary>
    /// DeletePatient
    /// </summary>
    /// <remarks>
    /// Only patient that hasn't received any treatment can be deleted
    /// </remarks>
    [HttpDelete]
    [Route("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeletePatient(Guid id)
    {
        var result = await bus.InvokeAsync<Result>(new DeletePatientCommand(id));
        return result.ToApiResponse();
    }

    [HttpPost]
    [Route("{patientId:guid}/treatments")]
    [ProducesResponseType<Result<CreateTreatmentResult>>(StatusCodes.Status201Created)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePatientTreatment(Guid patientId, CreateTreatmentRequestDto req)
    {
        // Get doctor ID from request header. The auth middleware will check for this ID if it exist
        // and valid UUID, so we just need to get the ID here
        Guid.TryParse(Request.Headers["X-ID"], out var doctorId);
        logger.LogDebug("Gateway headers stream down: {headers}", new
        {
            Id = Request.Headers["X-ID"],
            Email = Request.Headers["X-Email"],
            Role = Request.Headers["X-Role"],
        });
        var result = await bus.InvokeAsync<Result<CreateTreatmentResult>>(req.ToCommand(patientId, doctorId));
        return result.ToApiResponse();
    }

    [HttpGet]
    [Route("{patientId:guid}/treatments/{treatmentId:guid}")]
    [ProducesResponseType<Result<TreatmentInfo>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPatientTreatment(Guid patientId, Guid treatmentId)
    {
        var result = await bus.InvokeAsync<Result<TreatmentInfo>>(new GetTreatmentByIdQuery(treatmentId, patientId));
        return result.ToApiResponse();
    }
}
