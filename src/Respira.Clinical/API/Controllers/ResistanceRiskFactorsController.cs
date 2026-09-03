using Application.Features.ResistanceRiskFactors.DeleteResistanceRiskFactor;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Clinical.API.Dtos;
using Respira.ServiceDefaults.Contracts.Results;
using Respira.ServiceDefaults.Dtos;
using Wolverine;

namespace Respira.Clinical.API.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/resistance-risk-factors/{id:guid}")]
[ApiVersion("1.0")]
public class ResistanceRiskFactorsController(IMessageBus bus) : ControllerBase
{
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateResistanceRiskFactor(Guid id, [FromBody] UpdateResistanceRiskFactorRequestDto req)
    {
        var result = await bus.InvokeAsync<Result>(req.ToCommand(id));
        return result.ToApiResponse();
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveResistanceRiskFactor(Guid id)
    {
        var result = await bus.InvokeAsync<Result>(new DeleteResistanceRiskFactorCommand(id));
        return result.ToApiResponse();
    }
}