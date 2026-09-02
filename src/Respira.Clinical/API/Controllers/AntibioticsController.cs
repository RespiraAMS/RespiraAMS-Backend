using Application.Features.Antibiotics.AddDosage;
using Application.Features.Antibiotics.CreateAntibiotic;
using Application.Features.Antibiotics.DeleteAntibiotic;
using Application.Features.Antibiotics.DeleteDosage;
using Application.Features.Antibiotics.GetAntibioticById;
using Application.Features.Antibiotics.GetAntibiotics;
using Application.Features.Antibiotics.GetPagedAntibiotic;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Clinical.API.Dtos;
using Respira.ServiceDefaults.Dtos;
using Wolverine;

namespace Respira.Clinical.API.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/antibiotics")]
[ApiVersion("1.0")]
public class AntibioticsController(IMessageBus bus) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<Result<CreateAntibioticResult>>(StatusCodes.Status201Created)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateAntibiotic([FromBody] CreateAntibioticCommand req)
    {
        var result = await bus.InvokeAsync<CreateAntibioticResult>(req);
        var resp = Result<CreateAntibioticResult>.Ok(result, statusCode: StatusCodes.Status201Created);
        return Created((string?)null, resp);
    }

    [HttpGet]
    [ProducesResponseType<Result<Pagination<PagedAntibioticItem>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAntibiotics([FromQuery] GetPagedAntibioticsRequestDto req)
    {
        var result = await bus.InvokeAsync<Pagination<PagedAntibioticItem>>(req.ToQuery());
        var resp = Result<Pagination<PagedAntibioticItem>>.Ok(result);
        return Ok(resp);
    }

    [HttpGet]
    [Route("list")]
    [ProducesResponseType<Result<IEnumerable<AntibioticItem>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAntibiotics()
    {
        var result = await bus.InvokeAsync<GetAntibioticsResult>(new GetAntibioticsQuery());
        var resp = Result<IEnumerable<AntibioticItem>>.Ok(result.Antibiotics);
        return Ok(resp);
    }

    [HttpGet]
    [Route("{id:guid}")]
    [ProducesResponseType<Result<AntibioticResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAntibiotic(Guid id)
    {
        var result = await bus.InvokeAsync<AntibioticResult>(new GetAntibioticByIdQuery { Id = id });
        var resp = Result<AntibioticResult>.Ok(result);
        return Ok(resp);
    }

    [HttpPut]
    [Route("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateAntibiotic(Guid id, [FromBody] UpdateAntibioticRequestDto req)
    {
        await bus.InvokeAsync(req.ToCommand(id));
        return NoContent();
    }

    [HttpPut]
    [Route("{id:guid}/spectrum")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateAntibioticSpectrum(Guid id,
        [FromBody] UpdateAntibioticSpectrumRequestDto req)
    {
        await bus.InvokeAsync(req.ToCommand(id));
        return NoContent();
    }

    [HttpPost]
    [Route("{id:guid}/dosages")]
    [ProducesResponseType<Result<AddDosageResult>>(StatusCodes.Status201Created)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddDosage(Guid id, [FromBody] AddDosageRequestDto req)
    {
        var result = await bus.InvokeAsync<AddDosageResult>(req.ToCommand(id));
        var resp = Result<AddDosageResult>.Ok(result, statusCode: StatusCodes.Status201Created);
        return Created((string?)null, resp);
    }

    [HttpPut]
    [Route("{id:guid}/dosages/{dosageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateDosage(Guid id, Guid dosageId, [FromBody] UpdateDosageRequestDto req)
    {
        await bus.InvokeAsync(req.ToCommand(dosageId, id));
        return NoContent();
    }

    [HttpDelete]
    [Route("{id:guid}/dosages/{dosageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteDosage(Guid id, Guid dosageId)
    {
        await bus.InvokeAsync(new DeleteDosageCommand { Id = dosageId, AntibioticId = id });
        return NoContent();
    }

    [HttpDelete]
    [Route("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAntibiotic(Guid id)
    {
        await bus.InvokeAsync(new DeleteAntibioticCommand { Id = id });
        return NoContent();
    }
}
