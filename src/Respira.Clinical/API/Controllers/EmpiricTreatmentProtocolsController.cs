using Application.Features.EmpiricTreatmentProtocols.DeleteEmpiricTreatmentProtocol;
using Application.Features.EmpiricTreatmentProtocols.GetEmpiricTreatmentProtocolById;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Clinical.API.Dtos;
using Respira.ServiceDefaults.Dtos;
using Wolverine;

namespace Respira.Clinical.API.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/empiric-treatment-protocols/{id:guid}")]
[ApiVersion("1.0")]
public class EmpiricTreatmentProtocolsController(IMessageBus bus) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<Result<EmpiricTreatmentProtocolResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProtocol(Guid id)
    {
        var result = await bus
            .InvokeAsync<EmpiricTreatmentProtocolResult>(new GetEmpiricTreatmentProtocolByIdQuery { Id = id });
        var resp = Result<EmpiricTreatmentProtocolResult>.Ok(result);
        return Ok(resp);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateProtocol(Guid id, [FromBody] UpdateEmpiricTreatmentProtocolRequestDto req)
    {
        await bus.InvokeAsync(req.ToCommand(id));
        return NoContent();
    }

    [HttpPut]
    [Route("criteria")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddNewCriteria(Guid id, [FromBody] AddNewCriteriaRequestDto req)
    {
        await bus.InvokeAsync(req.ToCommand(id));
        return NoContent();
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteTreatmentProtocol(Guid id)
    {
        await bus.InvokeAsync(new DeleteEmpiricTreatmentProtocolCommand { Id = id });
        return NoContent();
    }
}