using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Clinical.API.Dtos;
using Respira.Clinical.Application.Features.SuspectedCauses.CreateSuspectedCause;
using Respira.Clinical.Application.Features.SuspectedCauses.DeleteSuspectedCause;
using Respira.Clinical.Application.Features.SuspectedCauses.GetPagedSuspectedCause;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Clinical.API.Controllers
{
    [ApiController]
    [Route("api/{version:apiVersion}/suspected-causes")]
    [ApiVersion("1.0")]
    public class SuspectedCausesController(IMessageBus bus) : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType<Result<CreateSuspectedCauseResult>>(StatusCodes.Status201Created)]
        [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateSuspectedCause([FromBody] CreateSuspectedCauseCommand req)
        {
            var result = await bus.InvokeAsync<Result<CreateSuspectedCauseResult>>(req);
            return result.ToApiResponse();
        }

        [HttpGet]
        [ProducesResponseType<Result<Pagination<PagedSuspectedCauseItem>>>(StatusCodes.Status200OK)]
        [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSuspectedCauses([FromQuery] GetPagedSuspectedCauseRequestDto req)
        {
            var result = await bus.InvokeAsync<Result<Pagination<PagedSuspectedCauseItem>>>(req.ToQuery());
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
        public async Task<IActionResult> UpdateSuspectedCause(Guid id, [FromBody] UpdateSuspectedCauseRequestDto req)
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
        public async Task<IActionResult> DeleteSuspectedCause(Guid id)
        {
            var result = await bus.InvokeAsync<Result>(new DeleteSuspectedCauseCommand { Id = id });
            return result.ToApiResponse();
        }
    }
}
