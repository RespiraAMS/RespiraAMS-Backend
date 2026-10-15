using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Patient.Application.Features.Patients.CreatePatient;
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
    }
}
