using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Clinical.API.Dtos;
using Respira.Clinical.Application.Features.Criteria.CreateCriterion;
using Respira.Clinical.Application.Features.Criteria.DeleteCriterion;
using Respira.Clinical.Application.Features.Criteria.GetCriteria;
using Respira.Clinical.Application.Features.Criteria.GetPagedCriterion;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Clinical.API.Controllers
{
    [ApiController]
    [Route("api/{version:apiVersion}/criteria")]
    [ApiVersion("1.0")]
    public class CriteriaController(IMessageBus bus) : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType<Result<CreateCriterionResult>>(StatusCodes.Status201Created)]
        [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateCriterion([FromBody] CreateCriterionCommand req)
        {
            var result = await bus.InvokeAsync<Result<CreateCriterionResult>>(req);
            return result.ToApiResponse();
        }

        [HttpGet]
        [ProducesResponseType<Result<Pagination<PagedCriterionItem>>>(StatusCodes.Status200OK)]
        [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPagedCriterion([FromQuery] GetPagedCriterionRequestDto req)
        {
            var result = await bus.InvokeAsync<Result<Pagination<PagedCriterionItem>>>(req.ToQuery());
            return result.ToApiResponse();
        }

        [HttpGet]
        [Route("list")]
        public async Task<IActionResult> ListAsync()
        {
            var criteria = await bus.InvokeAsync<Result<GetCriteriaResult>>(new GetCriteriaQuery());
            return criteria.ToApiResponse();
        }

        [HttpPut]
        [Route("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateCriterion(Guid id, [FromBody] UpdateCriterionRequestDto req)
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
        public async Task<IActionResult> DeleteCriterion(Guid id)
        {
            var result = await bus.InvokeAsync<Result>(new DeleteCriterionCommand { Id = id });
            return result.ToApiResponse();
        }
    }
}
