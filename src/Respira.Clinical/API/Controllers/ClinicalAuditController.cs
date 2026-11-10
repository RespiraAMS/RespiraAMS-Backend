using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Clinical.Application.Features.Audit.Rollback;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.API.Controllers;

/// <summary>
/// Access to the local Clinical audit trail and rollback tooling. The global
/// Clinical authorization middleware restricts these routes to the Admin role.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/clinical/audit-logs")]
public sealed class ClinicalAuditController(IClinicalRollbackService rollbackService) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var entry = await rollbackService.GetAsync(id, cancellationToken);
        return entry is null ? NotFound() : Ok(entry);
    }

    [HttpPost("{id:guid}/rollback/preview")]
    public async Task<IActionResult> Preview(Guid id, CancellationToken cancellationToken)
    {
        var preview = await rollbackService.PreviewAsync(id, cancellationToken);
        return preview is null ? NotFound() : Ok(preview);
    }

    [HttpPost("{id:guid}/rollback")]
    public async Task<IActionResult> Rollback(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await rollbackService.ApplyAsync(id, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ClinicalRollbackConflictException exception)
        {
            return Conflict(exception.Preview);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(
                Result.Failure(
                    new ApplicationError(ApplicationStatus.BadRequest, exception.Message)
                )
            );
        }
    }
}
