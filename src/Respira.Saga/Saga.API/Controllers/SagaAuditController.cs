using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Respira.Saga.Application.Features.Audit.Queries.Search;
using Respira.Saga.Application.Features.Audit.Queries.Search.Result;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Saga.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/audit-logs")]
[Authorize(Roles = "Admin")]
public sealed class SagaAuditController(IMessageBus bus) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(Result<Pagination<SagaAuditResult>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<Pagination<SagaAuditResult>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] Guid? actorAccountId,
        [FromQuery] string? action,
        [FromQuery] string? entityType,
        [FromQuery] Guid? entityId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default
    )
    {
        var result = await bus.InvokeAsync<Result<Pagination<SagaAuditResult>>>(
            new SearchSagaAuditQuery
            {
                ActorAccountId = actorAccountId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                From = from,
                To = to,
                Param = new PaginationParam { Page = page, Size = pageSize },
            },
            cancellationToken
        );
        return result.ToApiResponse();
    }
}
