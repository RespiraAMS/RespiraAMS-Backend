using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Clinical.API.Dtos;
using Respira.Clinical.Application.Features.Diagnosis.Diagnose;
using Respira.Clinical.Application.Features.Diagnosis.GetDiagnoseForm;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Clinical.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/diagnose")]
    [ApiVersion("1.0")]
    public class DiagnoseController(IMessageBus bus, ILogger<DiagnoseController> logger) : ControllerBase
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
        public async Task<IActionResult> ConfirmTreatment([FromBody] ConfirmTreatmentRequestDto req)
        {
            // Get doctor ID from the claims
            var doctorIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (doctorIdRaw is null)
            {
                logger.LogDebug("Failed to get doctor ID from claims");
                return Result
                    .Failure(new ApplicationError(ApplicationStatus.Unauthorized, "Unauthorized access"))
                    .ToApiResponse();
            }

            if (Guid.TryParse(doctorIdRaw, out var doctorId))
            {
                var result = await bus.InvokeAsync<Result>(req.ToQuery(doctorId));
                return result.ToApiResponse();
            }

            logger.LogDebug("Value in X-ID is not valid UUID");
            return Result
                .Failure(new ApplicationError(ApplicationStatus.Unauthorized, "Unauthorized access"))
                .ToApiResponse();
        }
    }
}
