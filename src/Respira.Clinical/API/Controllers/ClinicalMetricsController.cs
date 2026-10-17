using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Clinical.API.Dtos;
using Respira.Clinical.Application.Features.ClinicalMetrics.AddMetricRule;
using Respira.Clinical.Application.Features.ClinicalMetrics.CreateClinicalMetric;
using Respira.Clinical.Application.Features.ClinicalMetrics.DeleteClinicalMetric;
using Respira.Clinical.Application.Features.ClinicalMetrics.DeleteMetricRule;
using Respira.Clinical.Application.Features.ClinicalMetrics.GetClinicalMetricById;
using Respira.Clinical.Application.Features.ClinicalMetrics.GetMetricRuleById;
using Respira.Clinical.Application.Features.ClinicalMetrics.GetPagedClinicalMetric;
using Respira.Clinical.Application.Features.ClinicalMetrics.SearchClinicalMetric;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Clinical.API.Controllers
{
    [ApiController]
    [Route("api/{version:apiVersion}/clinical-metrics")]
    [ApiVersion("1.0")]
    public class ClinicalMetricsController(IMessageBus bus) : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType<Result<CreateClinicalMetricResult>>(StatusCodes.Status201Created)]
        [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateClinicalMetric([FromBody] CreateClinicalMetricCommand req)
        {
            var result = await bus.InvokeAsync<Result<CreateClinicalMetricResult>>(req);
            return result.ToApiResponse();
        }

        [HttpGet]
        [ProducesResponseType<Result<Pagination<PagedClinicalMetricItem>>>(StatusCodes.Status200OK)]
        [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPagedClinicalMetric([FromQuery] GetPagedClinicalMetricRequestDto req)
        {
            var result = await bus.InvokeAsync<Result<Pagination<PagedClinicalMetricItem>>>(req.ToQuery());
            return result.ToApiResponse();
        }

        [HttpGet]
        [Route("search")]
        public async Task<IActionResult> SearchClinicalMetric([FromQuery] SearchClinicalMetricRequestDto req)
        {
            var result = await bus.InvokeAsync<Result<Pagination<SearchClinicalMetricItem>>>(req.ToQuery());
            return result.ToApiResponse();
        }

        [HttpGet]
        [Route("{id:guid}")]
        [ProducesResponseType<Result<ClinicalMetricResult>>(StatusCodes.Status200OK)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetClinicalMetricById(Guid id)
        {
            var result = await bus.InvokeAsync<Result<ClinicalMetricResult>>(new GetClinicalMetricByIdQuery { Id = id });
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
        public async Task<IActionResult> UpdateClinicalMetric(Guid id, [FromBody] UpdateClinicalMetricRequestDto req)
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
        public async Task<IActionResult> DeleteClinicalMetric(Guid id)
        {
            var result = await bus.InvokeAsync<Result>(new DeleteClinicalMetricCommand { Id = id });
            return result.ToApiResponse();
        }

        [HttpPost]
        [Route("{id:guid}/rules")]
        [ProducesResponseType<Result<AddMetricRuleResult>>(StatusCodes.Status201Created)]
        [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddRule(Guid id, [FromBody] AddMetricRuleRequestDto req)
        {
            var result = await bus.InvokeAsync<Result<AddMetricRuleResult>>(req.ToCommand(id));
            return result.ToApiResponse();
        }

        [HttpGet]
#pragma warning disable ASP0018 // Unused route parameter
        [Route("{metricId:guid}/rules/{ruleId:guid}")]
#pragma warning restore ASP0018 // Unused route parameter
        [ProducesResponseType<Result<MetricRuleItem>>(StatusCodes.Status200OK)]
        [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMetricRuleById(Guid ruleId)
        {
            var result = await bus.InvokeAsync<Result<MetricRuleItem>>(new GetMetricRuleByIdQuery { Id = ruleId });
            return result.ToApiResponse();
        }

        [HttpPut]
        [Route("{metricId:guid}/rules/{ruleId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateMetricRule(Guid metricId, Guid ruleId, [FromBody] UpdateMetricRuleRequestDto req)
        {
            var result = await bus.InvokeAsync<Result>(req.ToCommand(ruleId, metricId));
            return result.ToApiResponse();
        }

        [HttpDelete]
#pragma warning disable ASP0018 // Unused route parameter
        [Route("{metricId:guid}/rules/{ruleId:guid}")]
#pragma warning restore ASP0018 // Unused route parameter
        public async Task<IActionResult> DeleteMetricRule(Guid ruleId)
        {
            var result = await bus.InvokeAsync<Result>(new DeleteMetricRuleCommand { Id = ruleId });
            return result.ToApiResponse();
        }
    }
}
