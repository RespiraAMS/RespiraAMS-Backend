using Respira.ServiceDefaults.Constracts.CQRS;

namespace Respira.SagaAudit.Application.Features.DeleteDoctor.Commands;

/// <summary>
/// Starts the DeleteDoctor saga: removes the avatar, the doctor profile and finally the
/// auth account, in that order. Issued by a manager/admin doctor.
/// </summary>
public record DeleteDoctorByManagerCommand : ICommand
{
    public required Guid ManagerDoctorId { get; init; }
    public required Guid AuthUserId { get; init; }
    public required Guid DoctorId { get; init; }
    public required Guid MediaId { get; init; }
}
