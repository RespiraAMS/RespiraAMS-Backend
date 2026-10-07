using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Clinical.API.Dtos;
using Respira.Clinical.Application.Features.ClinicalVariables.CreateClinicalVariable;
using Respira.Clinical.Application.Features.ClinicalVariables.DeleteClinicalVariable;
using Respira.Clinical.Application.Features.ClinicalVariables.GetClinicalVariableById;
using Respira.Clinical.Application.Features.ClinicalVariables.GetClinicalVariables;
using Respira.Clinical.Application.Features.ClinicalVariables.GetPagedClinicalVariable;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Clinical.API.Controllers
{
    [ApiController]
    [Route("api/{version:apiVersion}/clinical-variables")]
    [ApiVersion("1.0")]
    public class ClinicalVariablesController(IMessageBus bus) : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType<Result<CreateClinicalVariableResult>>(StatusCodes.Status201Created)]
        [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateClinicalVariable([FromBody] CreateClinicalVariableCommand req)
        {
            var result = await bus.InvokeAsync<Result<CreateClinicalVariableResult>>(req);
            return result.ToApiResponse();
        }

        [HttpGet]
        [ProducesResponseType<Result<Pagination<PagedClinicalVariableItem>>>(StatusCodes.Status200OK)]
        [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]

        public async Task<IActionResult> GetPagedClinicalVariables([FromQuery] GetPagedClinicalVariableRequestDto req)
        {
            var result = await bus.InvokeAsync<Result<Pagination<PagedClinicalVariableItem>>>(req.ToQuery());
            return result.ToApiResponse();
        }

        [HttpGet]
        [Route("list")]
        [ProducesResponseType<Result<GetClinicalVariablesResult>>(StatusCodes.Status200OK)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetClinicalVariables()
        {
            var result = await bus.InvokeAsync<Result<GetClinicalVariablesResult>>(new GetClinicalVariablesQuery());
            return result.ToApiResponse();
        }

        [HttpGet]
        [Route("{id:guid}")]
        [ProducesResponseType<Result<ClinicalVariableResult>>(StatusCodes.Status200OK)]
        [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetClinicalVariableById(Guid id)
        {
            var result = await bus.InvokeAsync<Result<ClinicalVariableResult>>(new GetClinicalVariableByIdQuery { Id = id });
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
        public async Task<IActionResult> UpdateClinicalVariable(Guid id, [FromBody] UpdateClinicalVariableRequestDto req)
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
        public async Task<IActionResult> DeleteClinicalVariable(Guid id)
        {
            var result = await bus.InvokeAsync<Result>(new DeleteClinicalVariableCommand { Id = id });
            return result.ToApiResponse();
        }
    }
}
