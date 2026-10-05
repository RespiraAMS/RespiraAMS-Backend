using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Clinical.Application.Features.Diagnosis.EmpiricalDiagnosis.Diagnose;
using Respira.Clinical.Application.Features.Diagnosis.EmpiricalDiagnosis.GetDiagnosisForm;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Clinical.API.Controllers
{
    [ApiController]
    [Route("api/{version:apiVersion}/diagnose")]
    [ApiVersion("1.0")]
    public class DiagnoseController(IMessageBus bus) : ControllerBase
    {
        [HttpGet]
        [Route("empirical")]
        public async Task<IActionResult> GetEmpiricalDiagnosisForm()
        {
            var result = await bus.InvokeAsync<Result<GetDiagnosisFormResult>>(new GetDiagnosisFormQuery());
            return result.ToApiResponse();
        }

        [HttpPost]
        [Route("empirical")]
        public async Task<IActionResult> PostEmpiricalDiagnosisForm([FromBody] DiagnoseQuery request)
        {
            var result = await bus.InvokeAsync<Result<DiagnoseResult>>(request);
            return result.ToApiResponse();
        }
    }
}
