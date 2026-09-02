using Asp.Versioning;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Respira.SagaAudit.Application.Features.GetSaga.Queries;
using Respira.SagaAudit.Application.Features.ListSagas.Queries;
using Respira.ServiceDefaults.Dtos;
using Wolverine;

namespace Respira.SagaAudit.API.Controllers;

/// <summary>
/// Query saga execution history and status.
/// </summary>
[ApiController]
[Route("api/{version:apiVersion}/sagas")]
[ApiVersion("1.0")]
public class SagaController(IMessageBus bus) : ControllerBase
{
    /// <summary>
    /// Gets a saga by its ID.
    /// </summary>
    [HttpGet("{sagaId:guid}")]
    public async Task<IActionResult> GetById(Guid sagaId, CancellationToken cancellationToken)
    {
        var result = await bus.InvokeAsync<Result<GetSagaResult>>(
            new GetSagaQuery { SagaId = sagaId },
            cancellationToken
        );

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Lists recent sagas with optional status filter.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] SagaStatus? status,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default
    )
    {
        var result = await bus.InvokeAsync<Result<List<ListSagasResult>>>(
            new ListSagasQuery { Status = status, Limit = limit },
            cancellationToken
        );

        return StatusCode(result.StatusCode, result);
    }
}
