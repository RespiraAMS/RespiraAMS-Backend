using Application.Features.IcuHospitalizeCriteria.DeleteIcuHospitalizeCriterion;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Clinical.API.Dtos;
using Respira.ServiceDefaults.Contracts.Results;
using Respira.ServiceDefaults.Dtos;
using Wolverine;

namespace Respira.Clinical.API.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/icu-hospitalize-criteria/{id:guid}")]
[ApiVersion("1.0")]
public class IcuHospitalizeCriteriaController(IMessageBus bus) : ControllerBase
{
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateIcuHospitalizeCriterion(Guid id, [FromBody] UpdateIcuHospitalizeCriterionRequestDto req)
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
    public async Task<IActionResult> RemoveIcuHospitalizeCriterion(Guid id)
    {
        var result = await bus.InvokeAsync<Result>(new DeleteIcuHospitalizeCriterionCommand(id));
        return result.ToApiResponse();
    }
}