using Application.Features.Causes.CreateCause;
using Application.Features.Diseases.GetDiseaseById;
using Application.Features.Diseases.GetDiseaseCriteria;
using Application.Features.Diseases.GetDiseases;
using Application.Features.Diseases.GetPagedDisease;
using Application.Features.EmpiricTreatmentProtocols.CreateEmpiricTreatmentProtocol;
using Application.Features.IcuHospitalizeCriteria.CreateIcuHospitalizeCriterion;
using Application.Features.ResistanceRiskFactors.CreateResistanceRiskFactor;
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
    [ProducesResponseType<Result<Pagination<PagedDiseaseItem>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDiseases([FromQuery] GetPagedDiseaseRequestDto req)
    {
        var result = await bus.InvokeAsync<Pagination<PagedDiseaseItem>>(req.ToQuery());
        var resp = Result<Pagination<PagedDiseaseItem>>.Ok(result, statusCode: StatusCodes.Status201Created);
        return Ok(resp);
    }

    [HttpGet]
    [Route("list")]
    [ProducesResponseType<Result<IEnumerable<DiseaseItem>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDiseases()
    {
        var result = await bus.InvokeAsync<GetDiseasesResult>(new GetDiseasesQuery());
        var resp = Result<IEnumerable<DiseaseItem>>.Ok(result.Diseases);
        return Ok(resp);
    }

    [HttpGet]
    [Route("{id:guid}")]
    [ProducesResponseType<Result<DiseaseResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDisease(Guid id)
    {
        var result = await bus.InvokeAsync<DiseaseResult>(new GetDiseaseByIdQuery { Id = id });
        var resp = Result<DiseaseResult>.Ok(result);
        return Ok(resp);
    }

    [HttpGet]
    [Route("{id:guid}/criteria")]
    [ProducesResponseType<Result<DiseaseCriteriaResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDiagnosisTemplate(Guid id)
    {
        var result = await bus.InvokeAsync<DiseaseCriteriaResult>(new GetDiseaseCriteriaQuery { Id = id });
        var resp = Result<DiseaseCriteriaResult>.Ok(result);
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
    public async Task<IActionResult> UpdateDisease(Guid id, [FromBody] UpdateDiseaseRequestDto req)
    {
        await bus.InvokeAsync(req.ToCommand(id));
        return NoContent();
    }

    [HttpPost]
    [Route("{id:guid}/causes")]
    [ProducesResponseType<Result<CreateCauseResult>>(StatusCodes.Status201Created)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddCause(Guid id, [FromBody] CreateCauseRequestDto req)
    {
        var result = await bus.InvokeAsync<CreateCauseResult>(req.ToCommand(id));
        var resp = Result<CreateCauseResult>.Ok(result, statusCode: StatusCodes.Status201Created);
        return Created((string?)null, resp);
    }

    [HttpPost]
    [Route("{id:guid}/icu-hospitalize-criteria")]
    [ProducesResponseType<Result<CreateIcuHospitalizeCriterionResult>>(StatusCodes.Status201Created)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddIcuHospitalizeCriterion(Guid id,
        [FromBody] CreateIcuHospitalizeCriterionRequestDto req)
    {
        var result = await bus.InvokeAsync<CreateIcuHospitalizeCriterionResult>(req.ToCommand(id));
        var resp = Result<CreateIcuHospitalizeCriterionResult>
            .Ok(result, statusCode: StatusCodes.Status201Created);
        return Created((string?)null, resp);
    }

    [HttpPost]
    [Route("{id:guid}/resistance-risk-factors")]
    [ProducesResponseType<Result<CreateResistanceRiskFactorResult>>(StatusCodes.Status201Created)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddResistanceRiskFactor(Guid id,
        [FromBody] CreateResistanceRiskFactorRequestDto req)
    {
        var result = await bus.InvokeAsync<CreateResistanceRiskFactorResult>(req.ToCommand(id));
        var resp = Result<CreateResistanceRiskFactorResult>
            .Ok(result, statusCode: StatusCodes.Status201Created);
        return Created((string?)null, resp);
    }

    [HttpPost]
    [Route("{id:guid}/treatment-protocols")]
    [ProducesResponseType<Result<CreateEmpiricTreatmentProtocolResult>>(StatusCodes.Status201Created)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddTreatmentProtocol(Guid id,
        [FromBody] CreateEmpiricTreatmentProtocolRequestDto req)
    {
        var result = await bus.InvokeAsync<CreateEmpiricTreatmentProtocolResult>(req.ToCommand(id));
        var resp = Result<CreateEmpiricTreatmentProtocolResult>
            .Ok(result, statusCode: StatusCodes.Status201Created);
        return Created((string?)null, resp);
    }
}