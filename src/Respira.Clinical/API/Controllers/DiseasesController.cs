using Application.Features.Diseases.GetDiseaseById;
using Application.Features.Diseases.GetDiseaseCriteria;
using Application.Features.Diseases.GetDiseases;
using Application.Features.Diseases.GetPagedDisease;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Clinical.API.Dtos;
using Respira.ServiceDefaults.Dtos;
using Wolverine;

namespace Respira.Clinical.API.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/diseases")]
[ApiVersion("1.0")]
public class DiseasesController(IMessageBus bus) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ApiResponse<Pagination<PagedDiseaseItem>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDiseases([FromQuery] GetPagedDiseaseRequestDto req)
    {
        var result = await bus.InvokeAsync<Pagination<PagedDiseaseItem>>(req.ToQuery());
        var resp = ApiResponse<Pagination<PagedDiseaseItem>>.Ok(result);
        return Ok(resp);
    }

    [HttpGet]
    [Route("list")]
    [ProducesResponseType<ApiResponse<IEnumerable<DiseaseItem>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDiseases()
    {
        var result = await bus.InvokeAsync<GetDiseasesResult>(new GetDiseasesQuery());
        var resp = ApiResponse<IEnumerable<DiseaseItem>>.Ok(result.Diseases);
        return Ok(resp);
    }

    [HttpGet]
    [Route("{id:guid}")]
    [ProducesResponseType<ApiResponse<DiseaseResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDisease(Guid id)
    {
        var result = await bus.InvokeAsync<DiseaseResult>(new GetDiseaseByIdQuery { Id = id });
        var resp = ApiResponse<DiseaseResult>.Ok(result);
        return Ok(resp);
    }

    [HttpGet]
    [Route("{id:guid}/criteria")]
    [ProducesResponseType<ApiResponse<DiseaseCriteriaResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDiagnosisTemplate(Guid id)
    {
        var result = await bus.InvokeAsync<DiseaseCriteriaResult>(new GetDiseaseCriteriaQuery { Id = id });
        var resp = ApiResponse<DiseaseCriteriaResult>.Ok(result);
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
    public async Task<IActionResult> UpdateDisease(Guid id, [FromBody] UpdateDiseaseRequestDto req)
    {
        await bus.InvokeAsync(req.ToCommand(id));
        return NoContent();
    }
}