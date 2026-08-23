using System.Text.Json;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Respira.SagaAudit.Application.Abstracts.Data;

namespace Respira.SagaAudit.Application.Services;

/// <summary>
/// Service for tracking saga execution progress.
/// </summary>
public class ProcessTrackerService(ISagaAuditDbContext dbContext)
{
    /// <summary>
    /// Creates a new process tracker for a saga.
    /// </summary>
    public async Task<ProcessTracker> CreateAsync(
        Guid sagaId,
        string sagaType,
        Guid? managerId,
        Guid? targetDoctorId,
        CancellationToken cancellationToken = default)
    {
        var tracker = new ProcessTracker
        {
            SagaId = sagaId,
            SagaType = sagaType,
            Status = SagaStatus.Running,
            CurrentStep = "Start",
            ManagerId = managerId,
            TargetDoctorId = targetDoctorId,
            StepsJson = JsonSerializer.Serialize(new List<StepRecord>
            {
                new() { Step = "Start", Timestamp = DateTimeOffset.UtcNow }
            }),
        };

        dbContext.ProcessTrackers.Add(tracker);
        await dbContext.SaveChangesAsync(cancellationToken);
        return tracker;
    }

    /// <summary>
    /// Updates the current step of a saga.
    /// </summary>
    public async Task UpdateStepAsync(
        Guid sagaId,
        string step,
        CancellationToken cancellationToken = default)
    {
        var tracker = await dbContext.ProcessTrackers
            .FirstOrDefaultAsync(x => x.SagaId == sagaId, cancellationToken);
        if (tracker is null) return;

        tracker.CurrentStep = step;
        var steps = JsonSerializer.Deserialize<List<StepRecord>>(tracker.StepsJson ?? "[]") ?? [];
        steps.Add(new StepRecord { Step = step, Timestamp = DateTimeOffset.UtcNow });
        tracker.StepsJson = JsonSerializer.Serialize(steps);
        tracker.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Marks a saga as completed.
    /// </summary>
    public async Task CompleteAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        var tracker = await dbContext.ProcessTrackers
            .FirstOrDefaultAsync(x => x.SagaId == sagaId, cancellationToken);
        if (tracker is null) return;

        tracker.Status = SagaStatus.Completed;
        tracker.CurrentStep = "Completed";
        tracker.UpdatedAt = DateTimeOffset.UtcNow;

        var steps = JsonSerializer.Deserialize<List<StepRecord>>(tracker.StepsJson ?? "[]") ?? [];
        steps.Add(new StepRecord { Step = "Completed", Timestamp = DateTimeOffset.UtcNow });
        tracker.StepsJson = JsonSerializer.Serialize(steps);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Marks a saga as failed with a reason.
    /// </summary>
    public async Task FailAsync(
        Guid sagaId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var tracker = await dbContext.ProcessTrackers
            .FirstOrDefaultAsync(x => x.SagaId == sagaId, cancellationToken);
        if (tracker is null) return;

        tracker.Status = SagaStatus.Failed;
        tracker.FailureReason = reason;
        tracker.UpdatedAt = DateTimeOffset.UtcNow;

        var steps = JsonSerializer.Deserialize<List<StepRecord>>(tracker.StepsJson ?? "[]") ?? [];
        steps.Add(new StepRecord { Step = $"Failed: {reason}", Timestamp = DateTimeOffset.UtcNow });
        tracker.StepsJson = JsonSerializer.Serialize(steps);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Marks a saga as compensating (rolling back).
    /// </summary>
    public async Task CompensateAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        var tracker = await dbContext.ProcessTrackers
            .FirstOrDefaultAsync(x => x.SagaId == sagaId, cancellationToken);
        if (tracker is null) return;

        tracker.Status = SagaStatus.Compensating;
        tracker.CurrentStep = "Compensating";
        tracker.UpdatedAt = DateTimeOffset.UtcNow;

        var steps = JsonSerializer.Deserialize<List<StepRecord>>(tracker.StepsJson ?? "[]") ?? [];
        steps.Add(new StepRecord { Step = "Compensating", Timestamp = DateTimeOffset.UtcNow });
        tracker.StepsJson = JsonSerializer.Serialize(steps);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Record of a saga step execution.
/// </summary>
public class StepRecord
{
    /// <summary>Name of the step executed (or a status marker such as "Completed"/"Failed").</summary>
    public required string Step { get; set; }
    /// <summary>UTC time at which the step was recorded.</summary>
    public DateTimeOffset Timestamp { get; set; }
}
