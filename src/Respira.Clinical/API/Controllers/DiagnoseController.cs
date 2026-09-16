using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Application.Features.Diagnose.EmpiricalDiagnose.GetDiagnoseForm;
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
            var result = await bus.InvokeAsync<Result<GetDiagnoseFormResult>>(new GetDiagnoseFormQuery());
            return result.ToApiResponse();
        }
    }
}
