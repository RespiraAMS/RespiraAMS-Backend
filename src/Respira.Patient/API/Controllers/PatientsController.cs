using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Patient.API.Dtos;
using Respira.Patient.Application.Features.Patients.AddNextOfKin;
using Respira.Patient.Application.Features.Patients.CreatePatient;
using Respira.Patient.Application.Features.Patients.DeleteNextOfKin;
using Respira.Patient.Application.Features.Patients.DeletePatient;
using Respira.Patient.Application.Features.Patients.DischargePatient;
using Respira.Patient.Application.Features.Patients.GetPagedPatient;
using Respira.Patient.Application.Features.Patients.GetPatientById;
using Respira.Patient.Application.Features.Patients.SearchPatient;
using Respira.Patient.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Patient.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/patients")]
    [ApiVersion("1.0")]
    public class PatientsController(IMessageBus bus) : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType<Result<CreatePatientResult>>(StatusCodes.Status201Created)]
        [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreatePatient([FromBody] CreatePatientCommand req)
        {
            var result = await bus.InvokeAsync<Result<CreatePatientResult>>(req);
            return result.ToApiResponse();
        }

        [HttpGet]
        [ProducesResponseType<Result<Pagination<PagedPatientItem>>>(StatusCodes.Status200OK)]
        [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPagedPatient([FromQuery] GetPagedPatientRequestDto req)
        {
            var result = await bus.InvokeAsync<Result<Pagination<PagedPatientItem>>>(req.ToQuery());
            return result.ToApiResponse();
        }

        [HttpGet]
        [Route("{id:guid}")]
        [ProducesResponseType<Result<PatientResult>>(StatusCodes.Status200OK)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPatientById(Guid id)
        {
            var result = await bus.InvokeAsync<Result<PatientResult>>(new GetPatientByIdQuery { Id = id });
            return result.ToApiResponse();
        }

        [HttpGet]
        [Route("search")]
        public async Task<IActionResult> SearchPatient([FromQuery] SearchPatientRequestDto req)
        {
            var result = await bus.InvokeAsync<Result<Pagination<SearchPatientItem>>>(req.ToQuery());
            return result.ToApiResponse();
        }

        [HttpPut]
        [Route("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdatePatient(Guid id, [FromBody] UpdatePatientRequestDto req)
        {
            var result = await bus.InvokeAsync<Result>(req.ToCommand(id));
            return result.ToApiResponse();
        }

        [HttpPut]
        [Route("{id:guid}/discharge")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DischargePatient(Guid id, [FromQuery] TreatmentOutcome outcome)
        {
            var result = await bus.InvokeAsync<Result>(new DischargePatientCommand { Id = id, Outcome = outcome });
            return result.ToApiResponse();
        }

        [HttpPost]
        [Route("{id:guid}/next-of-kin")]
        [ProducesResponseType<Result<AddNextOfKinResult>>(StatusCodes.Status201Created)]
        [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddNextOfKin(Guid id, [FromBody] AddNextOfKinRequestDto req)
        {
            var result = await bus.InvokeAsync<Result<AddNextOfKinResult>>(req.ToCommand(id));
            return result.ToApiResponse();
        }

        [HttpPut]
        [Route("/next-of-kin/{nextOfKinId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateNextOfKin(Guid nextOfKinId, [FromBody] UpdateNextOfKinRequestDto req)
        {
            var result = await bus.InvokeAsync<Result>(req.ToCommand(nextOfKinId));
            return result.ToApiResponse();
        }

        [HttpDelete]
        [Route("/next-of-kin/{nextOfKinId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteNextOfKin(Guid nextOfKinId)
        {
            var result = await bus.InvokeAsync<Result>(new DeleteNextOfKinCommand { Id = nextOfKinId });
            return result.ToApiResponse();
        }

        [HttpDelete]
        [Route("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeletePatient(Guid id)
        {
            var result = await bus.InvokeAsync<Result>(new DeletePatientCommand { Id = id });
            return result.ToApiResponse();
        }

    }
}
