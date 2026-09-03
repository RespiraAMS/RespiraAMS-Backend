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
using Respira.ServiceDefaults.Contracts.Results;
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
        var result = await bus.InvokeAsync<Result<CreateAntibioticResult>>(req);
        return result.ToApiResponse();
    }

    [HttpGet]
    [ProducesResponseType<Result<Pagination<PagedAntibioticItem>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAntibiotics([FromQuery] GetPagedAntibioticsRequestDto req)
    {
        var result = await bus.InvokeAsync<Result<Pagination<PagedAntibioticItem>>>(req.ToQuery());
        return result.ToApiResponse();
    }

    [HttpGet]
    [Route("list")]
    [ProducesResponseType<Result<GetAntibioticsResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAntibiotics()
    {
        var result = await bus.InvokeAsync<Result<GetAntibioticsResult>>(new GetAntibioticsQuery());
        return result.ToApiResponse();
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
        var result = await bus.InvokeAsync<Result<AntibioticResult>>(new GetAntibioticByIdQuery(id));
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
    public async Task<IActionResult> UpdateAntibiotic(Guid id, [FromBody] UpdateAntibioticRequestDto req)
    {
        var result = await bus.InvokeAsync<Result>(req.ToCommand(id));
        return result.ToApiResponse();
    }

    [HttpPut]
    [Route("{id:guid}/spectrum")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateAntibioticSpectrum(Guid id, [FromBody] UpdateAntibioticSpectrumRequestDto req)
    {
        var result = await bus.InvokeAsync<Result>(req.ToCommand(id));
        return result.ToApiResponse();
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
        var result = await bus.InvokeAsync<Result<AddDosageResult>>(req.ToCommand(id));
        return result.ToApiResponse();
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
        var result = await bus.InvokeAsync<Result>(req.ToCommand(dosageId, id));
        return result.ToApiResponse();
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
        var result = await bus.InvokeAsync<Result>(new DeleteDosageCommand { Id = dosageId, AntibioticId = id });
        return result.ToApiResponse();
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
        var result = await bus.InvokeAsync<Result>(new DeleteAntibioticCommand(id));
        return result.ToApiResponse();
    }
}
