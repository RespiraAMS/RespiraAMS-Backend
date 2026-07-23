using Application.Features.Pathogens.CreatePathogen;
using Application.Features.Pathogens.DeletePathogen;
using Application.Features.Pathogens.GetPagedPathogen;
using Application.Features.Pathogens.GetPathogens;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Clinical.API.Dtos;
using Respira.ServiceDefaults.Dtos;
using Wolverine;

namespace Respira.Clinical.API.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/pathogens")]
[ApiVersion("1.0")]
public class PathogensController(IMessageBus bus) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ApiResponse<CreatePathogenResult>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePathogen([FromBody] CreatePathogenCommand req)
    {
        var result = await bus.InvokeAsync<CreatePathogenResult>(req);
        var resp = ApiResponse<CreatePathogenResult>
            .Ok(result, statusCode: StatusCodes.Status201Created);
        return Created((string?)null, resp);
    }

    [HttpGet]
    [ProducesResponseType<ApiResponse<Pagination<GetPagedPathogenResult>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPathogens([FromQuery] GetPagedPathogenRequestDto req)
    {
        var result = await bus.InvokeAsync<Pagination<GetPagedPathogenResult>>(req.ToQuery());
        var resp = ApiResponse<Pagination<GetPagedPathogenResult>>.Ok(result);
        return Ok(resp);
    }

    [HttpGet]
    [Route("list")]
    [ProducesResponseType<ApiResponse<IEnumerable<GetPathogensResult>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPathogens()
    {
        var result = await bus.InvokeAsync<ICollection<GetPathogensResult>>(new GetPathogensQuery());
        var resp = ApiResponse<IEnumerable<GetPathogensResult>>.Ok(result);
        return Ok(resp);
    }

    [HttpPut]
    [Route("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdatePathogen(Guid id, [FromBody] UpdatePathogenRequestDto req)
    {
        await bus.InvokeAsync(req.ToCommand(id));
        return NoContent();
    }

    [HttpDelete]
    [Route("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeletePathogen(Guid id)
    {
        await bus.InvokeAsync(new DeletePathogenCommand(id));
        return NoContent();
    }
}