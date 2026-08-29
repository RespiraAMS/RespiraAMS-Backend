using Application.Features.Diagnose.EmpiricalDiagnose;
using Application.Features.Diagnose.TargetedDiagnose;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.ServiceDefaults.Dtos;
using Wolverine;

namespace Respira.Clinical.API.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/diagnose")]
[ApiVersion("1.0")]
public class DiagnoseController(IMessageBus bus) : ControllerBase
{
    [HttpPost]
    [Route("empirical")]
    [ProducesResponseType<ApiResponse<EmpiricalDiagnoseResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EmpiricalDiagnose([FromBody] EmpiricalDiagnoseQuery req)
    {
        var result = await bus.InvokeAsync<EmpiricalDiagnoseResult>(req);
        var resp = ApiResponse<EmpiricalDiagnoseResult>.Ok(result);
        return Ok(resp);
    }

    [HttpPost]
    [Route("target")]
    [ProducesResponseType<ApiResponse<TargetedDiagnoseResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TargetedDiagnose([FromBody] TargetedDiagnoseQuery req)
    {
        var result = await bus.InvokeAsync<TargetedDiagnoseResult>(req);
        var resp = ApiResponse<TargetedDiagnoseResult>.Ok(result);
        return Ok(resp);
    }
}
