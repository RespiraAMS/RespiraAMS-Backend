using Respira.ServiceDefaults.Constracts.CQRS;

namespace Respira.SagaAudit.Application.Features.DeleteDoctor.Commands;

/// <summary>
/// Command issued from the HTTP boundary to start the DeleteDoctor saga slice.
/// Registers the process tracker and then dispatches
/// <see cref="DeleteDoctorByManagerCommand"/> to the saga.
/// </summary>
public record StartDeleteDoctorSagaCommand : ICommand
{
    /// <summary>Id of the manager/admin doctor initiating the deletion.</summary>
    public required Guid ManagerDoctorId { get; init; }

    /// <summary>The shared entity ID — same for both Auth and Doctor tables.</summary>
    public required Guid EntityId { get; init; }

    /// <summary>Avatar media id to remove.</summary>
    public required Guid MediaId { get; init; }
}
