using Application.Features.Diagnose;
using Asp.Versioning;
using Domain.Services.Dtos;
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
    [ProducesResponseType<ApiResponse<DiagnoseResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Diagnose([FromBody] DiagnoseQuery req)
    {
        var result = await bus.InvokeAsync<DiagnoseResult>(req);
        var resp = ApiResponse<DiagnoseResult>.Ok(result);
        return Ok(resp);
    }
}