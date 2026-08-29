using Domain.Enums;
using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Authentication.UpdateUser.Rollback.Commands;

/// <summary>
/// Reverts a previously applied AuthDoctor update back to its original values.
/// </summary>
public record RollbackUpdateAuthDoctorCommand : ICommand
{
    /// <summary>Correlation identifier of the UpdateUser saga.</summary>
    public required Guid SagaId { get; init; }

    /// <summary>Identifier of the auth doctor account to revert.</summary>
    public required Guid AuthUserId { get; init; }

    /// <summary>Previous email to restore on the account.</summary>
    public required string OldEmail { get; init; }

    /// <summary>Previous phone number to restore on the account.</summary>
    public required string OldPhone { get; init; }

    /// <summary>Previous role to restore on the account.</summary>
    public required RoleType OldRole { get; init; }
}
