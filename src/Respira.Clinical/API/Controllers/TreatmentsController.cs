using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Clinical.API.Dtos;
using Respira.Clinical.Application.Features.TreatmentProtocols.CreateTreatmentProtocol;
using Respira.Clinical.Application.Features.TreatmentProtocols.SearchTreatmentProtocol;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Clinical.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/treatments")]
    [ApiVersion("1.0")]
    public class TreatmentsController(IMessageBus bus) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTreatmentProtocolCommand request)
        {
            var result = await bus.InvokeAsync<Result<CreateTreatmentResult>>(request);
            return result.ToApiResponse();
        }

        [HttpPut]
        [Route("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTreatmentRequestDto request)
        {
            var result = await bus.InvokeAsync<Result>(request.ToCommand(id));
            return result.ToApiResponse();
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] SearchTreatmentProtocolQuery request)
        {
            var result = await bus.InvokeAsync<Result<SearchTreatmentResult>>(request);
            return result.ToApiResponse();
        }
    }
}
