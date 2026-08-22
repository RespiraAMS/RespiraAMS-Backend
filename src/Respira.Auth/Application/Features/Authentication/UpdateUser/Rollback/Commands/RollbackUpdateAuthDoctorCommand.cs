using Domain.Enums;
using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Authentication.UpdateUser.Rollback.Commands;

/// <summary>
/// Reverts a previously applied AuthDoctor update back to its original values.
/// </summary>
public record RollbackUpdateAuthDoctorCommand : ICommand
{
    public required Guid SagaId { get; init; }
    public required Guid AuthUserId { get; init; }
    public required string OldEmail { get; init; }
    public required string OldPhone { get; init; }
    public required RoleType OldRole { get; init; }
}
