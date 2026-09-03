using Application.Features.AntibioticGroups.CreateAntibioticGroup;
using Application.Features.AntibioticGroups.DeleteAntibioticGroup;
using Application.Features.AntibioticGroups.GetAntibioticGroups;
using Application.Features.AntibioticGroups.GetPagedAntibioticGroup;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Clinical.API.Dtos;
using Respira.ServiceDefaults.Contracts.Results;
using Respira.ServiceDefaults.Dtos;
using Wolverine;

namespace Respira.Clinical.API.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/antibiotic-groups")]
[ApiVersion("1.0")]
public class AntibioticGroupsController(IMessageBus bus) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<Result<CreateAntibioticGroupResult>>(StatusCodes.Status201Created)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateAntibioticGroup([FromBody] CreateAntibioticGroupCommand req)
    {
        var result = await bus.InvokeAsync<Result<CreateAntibioticGroupResult>>(req);
        return result.ToApiResponse();
    }

    [HttpGet]
    [ProducesResponseType<Result<Pagination<PagedAntibioticGroupItem>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPagedAntibioticGroup([FromQuery] GetPagedAntibioticGroupRequestDto req)
    {
        var result = await bus.InvokeAsync<Result<Pagination<PagedAntibioticGroupItem>>>(req.ToQuery());
        return result.ToApiResponse();
    }

    [HttpGet]
    [Route("list")]
    [ProducesResponseType<Result<GetAntibioticGroupsResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAntibioticGroups()
    {
        var result = await bus.InvokeAsync<Result<GetAntibioticGroupsResult>>(new GetAntibioticGroupsQuery());
        return result.ToApiResponse();
    }

    [HttpPut]
    [Route("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateAntibioticGroup(Guid id, [FromBody] UpdateAntibioticGroupRequestDto req)
    {
        var result = await bus.InvokeAsync<Result>(req.ToCommand(id));
        return result.ToApiResponse();
    }

    [HttpDelete]
    [Route("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAntibioticGroup(Guid id)
    {
        var result = await bus.InvokeAsync<Result>(new DeleteAntibioticGroupCommand(id));
        return result.ToApiResponse();
    }
}