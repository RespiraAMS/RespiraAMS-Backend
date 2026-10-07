using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Clinical.API.Dtos;
using Respira.Clinical.Application.Features.RiskFactors.CreateRiskFactor;
using Respira.Clinical.Application.Features.RiskFactors.DeleteRiskFactor;
using Respira.Clinical.Application.Features.RiskFactors.GetPagedRiskFactor;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Clinical.API.Controllers
{
    [ApiController]
    [Route("api/{version:apiVersion}/risk-factors")]
    [ApiVersion("1.0")]
    public class RiskFactorsController(IMessageBus bus) : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType<Result<CreateRiskFactorResult>>(StatusCodes.Status201Created)]
        [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateRiskFactor([FromBody] CreateRiskFactorCommand req)
        {
            var result = await bus.InvokeAsync<Result<CreateRiskFactorResult>>(req);
            return result.ToApiResponse();
        }

        [HttpGet]
        [ProducesResponseType<Result<Pagination<PagedRiskFactorItem>>>(StatusCodes.Status200OK)]
        [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPagedRiskFactor([FromQuery] GetPagedRiskFactorRequestDto req)
        {
            var result = await bus.InvokeAsync<Result<Pagination<PagedRiskFactorItem>>>(req.ToQuery());
            return result.ToApiResponse();
        }

        [HttpPut]
        [Route("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateRiskFactor(Guid id, [FromBody] UpdateRiskFactorRequestDto req)
        {
            var result = await bus.InvokeAsync<Result>(req.ToCommand(id));
            return result.ToApiResponse();
        }

        [HttpDelete]
        [Route("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteRiskFactor(Guid id)
        {
            var result = await bus.InvokeAsync<Result>(new DeleteRiskFactorCommand { Id = id });
            return result.ToApiResponse();
        }
    }
}
