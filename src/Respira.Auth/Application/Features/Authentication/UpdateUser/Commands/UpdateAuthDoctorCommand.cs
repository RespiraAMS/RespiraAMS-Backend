using Domain.Enums;
using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Authentication.UpdateUser.Commands;

/// <summary>
/// Updates an AuthDoctor's email/phone/role as part of the UpdateUser saga.
/// Old values are carried along so the saga can compensate by reverting.
/// </summary>
public record UpdateAuthDoctorCommand : ICommand
{
    public required Guid SagaId { get; init; }
    public required Guid AuthUserId { get; init; }

    // New values
    public required string Email { get; init; }
    public required string Phone { get; init; }
    public required RoleType Role { get; init; }

    // Old values (used for compensation)
    public required string OldEmail { get; init; }
    public required string OldPhone { get; init; }
    public required RoleType OldRole { get; init; }
}
