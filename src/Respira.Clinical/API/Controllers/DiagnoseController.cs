using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Clinical.Application.Features.Diagnosis.Diagnose;
using Respira.Clinical.Application.Features.Diagnosis.GetDiagnoseForm;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Clinical.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/diagnose")]
    [ApiVersion("1.0")]
    public class DiagnoseController(IMessageBus bus) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetEmpiricalDiagnosisForm()
        {
            var result = await bus.InvokeAsync<Result<DiagnoseFormResult>>(new GetDiagnoseFormQuery());
            return result.ToApiResponse();
        }

        [HttpPost]
        public async Task<IActionResult> PostEmpiricalDiagnosisForm([FromBody] DiagnoseQuery request)
        {
            var result = await bus.InvokeAsync<Result<DiagnoseResult>>(request);
            return result.ToApiResponse();
        }
    }
}
