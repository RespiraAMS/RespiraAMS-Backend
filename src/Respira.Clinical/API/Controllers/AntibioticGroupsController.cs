using Application.Features.AntibioticGroups.CreateAntibioticGroup;
using Application.Features.AntibioticGroups.DeleteAntibioticGroup;
using Application.Features.AntibioticGroups.GetAntibioticGroups;
using Application.Features.AntibioticGroups.GetPagedAntibioticGroup;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Clinical.API.Dtos;
using Respira.ServiceDefaults.Dtos;
using Wolverine;

namespace Respira.Clinical.API.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/antibiotic-groups")]
[ApiVersion("1.0")]
public class AntibioticGroupsController(IMessageBus bus) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ApiResponse<CreateAntibioticGroupResult>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateAntibioticGroup([FromBody] CreateAntibioticGroupCommand req)
    {
        var result = await bus.InvokeAsync<CreateAntibioticGroupResult>(req);
        var resp = ApiResponse<CreateAntibioticGroupResult>
            .Ok(result, statusCode: StatusCodes.Status201Created);
        return Created((string?)null, resp);
    }

    [HttpGet]
    [ProducesResponseType<ApiResponse<Pagination<PagedAntibioticGroupItem>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPagedAntibioticGroup([FromQuery] GetPagedAntibioticGroupRequestDto req)
    {
        var result = await bus.InvokeAsync<Pagination<PagedAntibioticGroupItem>>(req.ToQuery());
        var resp = ApiResponse<Pagination<PagedAntibioticGroupItem>>.Ok(result);
        return Ok(resp);
    }

    [HttpGet]
    [Route("list")]
    [ProducesResponseType<ApiResponse<IEnumerable<AntibioticGroupItem>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAntibioticGroups()
    {
        var result = await bus.InvokeAsync<GetAntibioticGroupsResult>(new GetAntibioticGroupsQuery());
        var resp = ApiResponse<IEnumerable<AntibioticGroupItem>>.Ok(result.AntibioticGroups);
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
    public async Task<IActionResult> UpdateAntibioticGroup(Guid id, [FromBody] UpdateAntibioticGroupRequestDto req)
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
    public async Task<IActionResult> DeleteAntibioticGroup(Guid id)
    {
        await bus.InvokeAsync(new DeleteAntibioticGroupCommand { Id = id });
        return NoContent();
    }
}