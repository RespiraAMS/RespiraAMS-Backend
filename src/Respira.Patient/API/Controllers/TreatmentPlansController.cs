using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Patient.API.Dtos;
using Respira.Patient.Application.Features.TreatmentPlans.GetTreatmentPlanById;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Patient.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/treatment-plans/{id:guid}")]
    [ApiVersion("1.0")]
    public class TreatmentPlansController(IMessageBus bus) : ControllerBase
    {
        [HttpPut]
        [Route("close")]
        public async Task<IActionResult> CloseTreatmentPlan(Guid id, [FromBody] CloseTreatmentPlanRequestDto req)
        {
            var result = await bus.InvokeAsync<Result>(req.ToCommand(id));
            return result.ToApiResponse();
        }

        [HttpGet]
        public async Task<IActionResult> GetTreatmentPlan(Guid id)
        {
            var result = await bus.InvokeAsync<Result<TreatmentPlanResult>>(new GetTreatmentPlanByIdQuery { Id = id });
            return result.ToApiResponse();
        }

        [HttpPut]
        [Route("review")]
        public async Task<IActionResult> ReviewTreatmentPlan(Guid id, [FromBody] ReviewTreatmentPlanRequestDto req)
        {
            var result = await bus.InvokeAsync<Result>(req.ToCommand(id));
            return result.ToApiResponse();
        }
    }
}
