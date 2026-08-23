using Respira.ServiceDefaults.Models;

namespace Domain.Entities;

/// <summary>
/// Tracks saga execution progress, steps completed, and failure reasons.
/// </summary>
public class ProcessTracker : Base
{
    /// <summary>Saga instance ID</summary>
    public required Guid SagaId { get; set; }

    /// <summary>Type of saga (CreateDoctor, UpdateDoctor, DeleteDoctor)</summary>
    public required string SagaType { get; set; }

    /// <summary>Current status: Running, Completed, Failed, Compensating</summary>
    public required SagaStatus Status { get; set; }

    /// <summary>Current step being executed</summary>
    public string? CurrentStep { get; set; }

    /// <summary>JSON array of completed steps with timestamps</summary>
    public string? StepsJson { get; set; }

    /// <summary>Failure reason if saga failed</summary>
    public string? FailureReason { get; set; }

    /// <summary>ID of the manager who initiated the saga</summary>
    public Guid? ManagerId { get; set; }

    /// <summary>ID of the doctor being created/updated/deleted</summary>
    public Guid? TargetDoctorId { get; set; }
}

/// <summary>
/// Saga execution status.
/// </summary>
public enum SagaStatus
{
    Running,
    Completed,
    Failed,
    Compensating,
}
