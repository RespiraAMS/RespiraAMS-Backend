using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Patient.API.Dtos;
using Respira.Patient.Application.Features.Patients.CreatePatient;
using Respira.Patient.Application.Features.Patients.DeletePatient;
using Respira.Patient.Application.Features.Patients.DischargePatient;
using Respira.Patient.Application.Features.Patients.GetPagedPatient;
using Respira.Patient.Application.Features.Patients.GetPatientById;
using Respira.Patient.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Patient.API.Controllers
{
    [ApiController]
    [Route("api/{version:apiVersion}/patients")]
    [ApiVersion("1.0")]
    public class PatientsController(IMessageBus bus) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreatePatient([FromBody] CreatePatientCommand req)
        {
            var result = await bus.InvokeAsync<Result<CreatePatientResult>>(req);
            return result.ToApiResponse();
        }

        [HttpGet]
        public async Task<IActionResult> GetPagedPatient([FromQuery] GetPagedPatientRequestDto req)
        {
            var result = await bus.InvokeAsync<Result<Pagination<PagedPatientItem>>>(req.ToQuery());
            return result.ToApiResponse();
        }

        [HttpGet]
        [Route("{id:guid}")]
        public async Task<IActionResult> GetPatientById(Guid id)
        {
            var result = await bus.InvokeAsync<Result<PatientResult>>(new GetPatientByIdQuery { Id = id });
            return result.ToApiResponse();
        }

        [HttpPut]
        [Route("{id:guid}")]
        public async Task<IActionResult> UpdatePatient(Guid id, [FromBody] UpdatePatientRequestDto req)
        {
            var result = await bus.InvokeAsync<Result>(req.ToCommand(id));
            return result.ToApiResponse();
        }

        [HttpDelete]
        [Route("{id:guid}")]
        public async Task<IActionResult> DeletePatient(Guid id)
        {
            var result = await bus.InvokeAsync<Result>(new DeletePatientCommand { Id = id });
            return result.ToApiResponse();
        }

        [HttpPut]
        [Route("{id:guid}/discharge")]
        public async Task<IActionResult> DischargePatient(Guid id, [FromQuery] TreatmentOutcome outcome)
        {
            var result = await bus.InvokeAsync<Result>(new DischargePatientCommand { Id = id, Outcome = outcome });
            return result.ToApiResponse();
        }
    }
}
