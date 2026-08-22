using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Authentication.CreateUser.Rollback.Commands;

/// <summary>
/// Compensates a previously created AuthDoctor account when a later saga step fails.
/// </summary>
public record RollbackCreateAuthDoctorCommand : ICommand
{
    public required Guid SagaId { get; init; }
    public required Guid AuthUserId { get; init; }
}
