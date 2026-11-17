using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Clinical.API.Dtos;
using Respira.Clinical.Application.Features.TreatmentProtocols.CreateTreatmentProtocol;
using Respira.Clinical.Application.Features.TreatmentProtocols.DeleteTreatmentProtocol;
using Respira.Clinical.Application.Features.TreatmentProtocols.GetPagedTreatmentProtocol;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Clinical.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/treatment-protocols")]
    [ApiVersion("1.0")]
    public class TreatmentProtocolsController(IMessageBus bus) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateTreatmentProtocol([FromBody] CreateTreatmentProtocolCommand request)
        {
            var result = await bus.InvokeAsync<Result<CreateTreatmentProtocolResult>>(request);
            return result.ToApiResponse();
        }

        [HttpPut]
        [Route("{id:guid}")]
        public async Task<IActionResult> UpdateTreatmentProtocol(Guid id, [FromBody] UpdateTreatmentProtocolRequestDto request)
        {
            var result = await bus.InvokeAsync<Result>(request.ToCommand(id));
            return result.ToApiResponse();
        }

        [HttpGet]
        public async Task<IActionResult> GetPagedTreatmentProtocol([FromQuery] GetPagedTreatmentProtocolRequestDto request)
        {
            var result = await bus.InvokeAsync<Result<Pagination<TreatmentProtocolItem>>>(request.ToQuery());
            return result.ToApiResponse();
        }

        [HttpDelete]
        [Route("{id:guid}")]
        public async Task<IActionResult> DeleteTreatmentProtocol(Guid id)
        {
            var result = await bus.InvokeAsync<Result>(new DeleteTreatmentProtocolCommand { Id = id });
            return result.ToApiResponse();
        }
    }
}
