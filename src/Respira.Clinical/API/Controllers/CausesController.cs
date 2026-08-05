using Application.Features.Causes.DeleteCause;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Clinical.API.Dtos;
using Wolverine;

namespace Respira.Clinical.API.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/causes/{id:guid}")]
[ApiVersion("1.0")]
public class CausesController(IMessageBus bus) : ControllerBase
{
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateCause(Guid id, [FromBody] UpdateCauseRequestDto req)
    {
        await bus.InvokeAsync(req.ToCommand(id));
        return NoContent();
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveCause(Guid id)
    {
        await bus.InvokeAsync(new DeleteCauseCommand { Id = id });
        return NoContent();
    }
}