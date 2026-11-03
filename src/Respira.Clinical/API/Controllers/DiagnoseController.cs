using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Clinical.Application.Features.Diagnosis.ConfirmTreatment;
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
        public async Task<IActionResult> GetDiagnosisForm()
        {
            var result = await bus.InvokeAsync<Result<DiagnoseFormResult>>(new GetDiagnoseFormQuery());
            return result.ToApiResponse();
        }

        [HttpPost]
        public async Task<IActionResult> Diagnose([FromBody] DiagnoseQuery req)
        {
            var result = await bus.InvokeAsync<Result<DiagnoseResult>>(req);
            return result.ToApiResponse();
        }

        [HttpPost]
        [Route("confirm-treatment")]
        public async Task<IActionResult> ConfirmTreatment([FromBody] ConfirmTreatmentQuery req)
        {
            var result = await bus.InvokeAsync<Result>(req);
            return result.ToApiResponse();
        }
    }
}
