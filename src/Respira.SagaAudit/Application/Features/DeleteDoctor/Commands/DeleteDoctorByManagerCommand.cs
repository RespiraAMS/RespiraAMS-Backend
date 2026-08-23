using Respira.ServiceDefaults.Constracts.CQRS;

namespace Respira.SagaAudit.Application.Features.DeleteDoctor.Commands;

/// <summary>
/// Starts the DeleteDoctor saga: removes the avatar, the doctor profile and finally the
/// auth account, in that order. Issued by a manager/admin doctor.
/// </summary>
public record DeleteDoctorByManagerCommand : ICommand
{
    /// <summary>Id of the manager/admin doctor initiating the deletion.</summary>
    public required Guid ManagerDoctorId { get; init; }
    /// <summary>Auth account id of the doctor to delete.</summary>
    public required Guid AuthUserId { get; init; }
    /// <summary>Doctor profile id to delete.</summary>
    public required Guid DoctorId { get; init; }
    /// <summary>Avatar media id to remove.</summary>
    public required Guid MediaId { get; init; }
}
