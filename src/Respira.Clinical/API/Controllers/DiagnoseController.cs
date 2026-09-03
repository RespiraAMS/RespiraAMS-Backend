using Application.Features.Diagnose.EmpiricalDiagnose;
using Application.Features.Diagnose.TargetedDiagnose;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Clinical.API.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/diagnose")]
[ApiVersion("1.0")]
public class DiagnoseController(IMessageBus bus) : ControllerBase
{
    [HttpPost]
    [Route("empirical")]
    [ProducesResponseType<Result<EmpiricalDiagnoseResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EmpiricalDiagnose([FromBody] EmpiricalDiagnoseQuery req)
    {
        var result = await bus.InvokeAsync<Result<EmpiricalDiagnoseResult>>(req);
        return Ok(result);
    }

    [HttpPost]
    [Route("target")]
    [ProducesResponseType<Result<TargetedDiagnoseResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TargetedDiagnose([FromBody] TargetedDiagnoseQuery req)
    {
        var result = await bus.InvokeAsync<Result<TargetedDiagnoseResult>>(req);
        return Ok(result);
    }
}
