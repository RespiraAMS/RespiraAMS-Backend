using Application.Abstracts.Data;
using Asp.Versioning;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Respira.SagaAudit.API.Controllers;

/// <summary>
/// Query saga execution history and status.
/// </summary>
[ApiController]
[Route("api/{version:apiVersion}/sagas")]
[ApiVersion("1.0")]
public class SagaController(ISagaAuditDbContext dbContext) : ControllerBase
{
    /// <summary>
    /// Gets a saga by its ID.
    /// </summary>
    [HttpGet("{sagaId:guid}")]
    public async Task<IActionResult> GetById(Guid sagaId, CancellationToken cancellationToken)
    {
        var tracker = await dbContext
            .ProcessTrackers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SagaId == sagaId, cancellationToken);

        if (tracker is null)
        {
            return NotFound();
        }

        return Ok(
            new
            {
                tracker.SagaId,
                tracker.SagaType,
                Status = tracker.Status.ToString(),
                tracker.CurrentStep,
                Steps = System.Text.Json.JsonSerializer.Deserialize<object[]>(
                    tracker.StepsJson ?? "[]"
                ),
                tracker.FailureReason,
                tracker.ManagerId,
                tracker.TargetDoctorId,
                tracker.CreatedAt,
                tracker.UpdatedAt,
            }
        );
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
        var query = dbContext.ProcessTrackers.AsNoTracking().AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        var trackers = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .Select(x => new
            {
                x.SagaId,
                x.SagaType,
                Status = x.Status.ToString(),
                x.CurrentStep,
                x.FailureReason,
                x.ManagerId,
                x.TargetDoctorId,
                x.CreatedAt,
                x.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        return Ok(trackers);
    }
}
