using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Clinical.API.Dtos;
using Respira.Clinical.Application.Features.Pathogens.CreatePathogen;
using Respira.Clinical.Application.Features.Pathogens.DeletePathogen;
using Respira.Clinical.Application.Features.Pathogens.GetPagedPathogen;
using Respira.Clinical.Application.Features.Pathogens.GetPathogens;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Clinical.API.Controllers
{
    [ApiController]
    [Route("api/{version:apiVersion}/pathogens")]
    [ApiVersion("1.0")]
    public class PathogensController(IMessageBus bus) : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType<Result<CreatePathogenResult>>(StatusCodes.Status201Created)]
        [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreatePathogen([FromBody] CreatePathogenCommand req)
        {
            var result = await bus.InvokeAsync<Result<CreatePathogenResult>>(req);
            return result.ToApiResponse();
        }

        [HttpGet]
        [ProducesResponseType<Result<Pagination<PagedPathogenItem>>>(StatusCodes.Status200OK)]
        [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPathogens([FromQuery] GetPagedPathogenRequestDto req)
        {
            var result = await bus.InvokeAsync<Result<Pagination<PagedPathogenItem>>>(req.ToQuery());
            return result.ToApiResponse();
        }

        [HttpGet]
        [Route("list")]
        [ProducesResponseType<Result<IEnumerable<PathogenItem>>>(StatusCodes.Status200OK)]
        [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPathogens()
        {
            var result = await bus.InvokeAsync<Result<GetPathogensResult>>(new GetPathogensQuery());
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
        public async Task<IActionResult> UpdatePathogen(Guid id, [FromBody] UpdatePathogenRequestDto req)
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
        public async Task<IActionResult> DeletePathogen(Guid id)
        {
            var result = await bus.InvokeAsync<Result>(new DeletePathogenCommand(id));
            return result.ToApiResponse();
        }
    }
}
