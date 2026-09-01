using Respira.ServiceDefaults.Contracts.CQRS;

namespace Application.Features.Authentication.DeleteUser.Commands;

/// <summary>
/// Deletes an AuthDoctor account. Terminal step of the DeleteUser saga (no compensation,
/// since the password hash cannot be restored).
/// </summary>
public record DeleteAuthDoctorCommand : ICommand
{
    /// <summary>Correlation identifier of the DeleteUser saga.</summary>
    public required Guid SagaId { get; init; }

    /// <summary>Identifier of the auth doctor account to delete.</summary>
    public required Guid AuthUserId { get; init; }
}
