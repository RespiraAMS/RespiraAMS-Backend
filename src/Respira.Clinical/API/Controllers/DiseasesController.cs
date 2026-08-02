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

    [HttpPost]
    [Route("{id:guid}/causes")]
    [ProducesResponseType<ApiResponse<CreateCauseResult>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddCause(Guid id, [FromBody] CreateCauseRequestDto req)
    {
        var result = await bus.InvokeAsync<CreateCauseResult>(req.ToCommand(id));
        var resp = ApiResponse<CreateCauseResult>.Ok(result, statusCode: StatusCodes.Status201Created);
        return Created((string?)null, resp);
    }

    [HttpPost]
    [Route("{id:guid}/icu-hospitalize-criteria")]
    [ProducesResponseType<ApiResponse<CreateIcuHospitalizeCriterionResult>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddIcuHospitalizeCriterion(Guid id,
        [FromBody] CreateIcuHospitalizeCriterionRequestDto req)
    {
        var result = await bus.InvokeAsync<CreateIcuHospitalizeCriterionResult>(req.ToCommand(id));
        var resp = ApiResponse<CreateIcuHospitalizeCriterionResult>
            .Ok(result, statusCode: StatusCodes.Status201Created);
        return Created((string?)null, resp);
    }

    [HttpPost]
    [Route("{id:guid}/resistance-risk-factors")]
    [ProducesResponseType<ApiResponse<CreateResistanceRiskFactorResult>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddResistanceRiskFactor(Guid id,
        [FromBody] CreateResistanceRiskFactorRequestDto req)
    {
        var result = await bus.InvokeAsync<CreateResistanceRiskFactorResult>(req.ToCommand(id));
        var resp = ApiResponse<CreateResistanceRiskFactorResult>
            .Ok(result, statusCode: StatusCodes.Status201Created);
        return Created((string?)null, resp);
    }

    [HttpPost]
    [Route("{id:guid}/treatment-protocols")]
    [ProducesResponseType<ApiResponse<CreateEmpiricTreatmentProtocolResult>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddTreatmentProtocol(Guid id,
        [FromBody] CreateEmpiricTreatmentProtocolRequestDto req)
    {
        var result = await bus.InvokeAsync<CreateEmpiricTreatmentProtocolResult>(req.ToCommand(id));
        var resp = ApiResponse<CreateEmpiricTreatmentProtocolResult>
            .Ok(result, statusCode: StatusCodes.Status201Created);
        return Created((string?)null, resp);
    }
}