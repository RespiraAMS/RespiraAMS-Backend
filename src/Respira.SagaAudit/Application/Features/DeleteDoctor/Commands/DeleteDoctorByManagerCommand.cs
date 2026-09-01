using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.SagaAudit.Application.Features.DeleteDoctor.Commands;

/// <summary>
/// Starts the DeleteDoctor saga: removes the avatar, the doctor profile and finally the
/// auth account, in that order. Issued by a manager/admin doctor.
/// </summary>
public record DeleteDoctorByManagerCommand : ICommand
{
    /// <summary>Id of the manager/admin doctor initiating the deletion.</summary>
    public required Guid ManagerDoctorId { get; init; }

    /// <summary>The shared entity ID — same for both Auth and Doctor tables (AuthUserId == DoctorId).</summary>
    public required Guid EntityId { get; init; }

    // Deprecated: derived from EntityId
    /// <summary>Deprecated — always equals EntityId.</summary>
    [Obsolete("Use EntityId instead")]
    public Guid AuthUserId => EntityId;

    /// <summary>Deprecated — always equals EntityId.</summary>
    [Obsolete("Use EntityId instead")]
    public Guid DoctorId => EntityId;

    /// <summary>Avatar media id to remove.</summary>
    public required Guid MediaId { get; init; }
}
