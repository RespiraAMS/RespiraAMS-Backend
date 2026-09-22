using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Clinical.Application.Features.Antibiotics.GetAntibiotics;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Clinical.API.Controllers
{
    [ApiController]
    [Route("api/{version:apiVersion}/antibiotics")]
    [ApiVersion("1.0")]
    public class AntibioticsController(IMessageBus bus) : ControllerBase
    {
        [HttpGet]
        [Route("list")]
        public async Task<IActionResult> List()
        {
            var result = await bus.InvokeAsync<Result<GetAntibioticsResult>>(new GetAntibioticQuery());
            return result.ToApiResponse();
        }
    }
}
