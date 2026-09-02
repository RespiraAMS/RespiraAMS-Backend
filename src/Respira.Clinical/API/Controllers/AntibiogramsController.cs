using Application.Features.Antibiograms.CreateAntibiogram;
using Application.Features.Antibiograms.DeleteAntibiogram;
using Application.Features.Antibiograms.GetPagedAntibiogram;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Clinical.API.Dtos;
using Respira.ServiceDefaults.Dtos;
using Wolverine;

namespace Respira.Clinical.API.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/antibiograms")]
[ApiVersion("1.0")]
public class AntibiogramsController(IMessageBus bus) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<Result<CreateAntibiogramResult>>(StatusCodes.Status201Created)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateAntibiogram([FromBody] CreateAntibiogramCommand req)
    {
        var result = await bus.InvokeAsync<CreateAntibiogramResult>(req);
        var resp = Result<CreateAntibiogramResult>.Ok(result, statusCode: StatusCodes.Status201Created);
        return Created((string?)null, resp);
    }

    [HttpGet]
    [ProducesResponseType<Result<Pagination<PagedAntibiogramItem>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPagedAntibiogram([FromQuery] GetPagedAntibiogramRequestDto req)
    {
        var result = await bus.InvokeAsync<Pagination<PagedAntibiogramItem>>(req.ToQuery());
        var resp = Result<Pagination<PagedAntibiogramItem>>.Ok(result);
        return Ok(resp);
    }

    [HttpPut]
    [Route("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateAntibiogram(Guid id, [FromBody] UpdateAntibiogramRequestDto req)
    {
        await bus.InvokeAsync(req.ToCommand(id));
        return NoContent();
    }

    [HttpDelete]
    [Route("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAntibiogram(Guid id)
    {
        await bus.InvokeAsync(new DeleteAntibiogramCommand { Id = id });
        return NoContent();
    }
}