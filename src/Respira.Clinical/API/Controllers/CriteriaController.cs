using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Clinical.Application.Features.Criteria.GetCriteria;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Clinical.API.Controllers
{
    [ApiController]
    [Route("api/{version:apiVersion}/criteria")]
    [ApiVersion("1.0")]
    public class CriteriaController(IMessageBus bus) : ControllerBase
    {
        [HttpGet]
        [Route("list")]
        public async Task<IActionResult> ListAsync()
        {
            var criteria = await bus.InvokeAsync<Result<GetCriteriaResult>>(new GetCriteriaQuery());
            return criteria.ToApiResponse();
        }
    }
}
