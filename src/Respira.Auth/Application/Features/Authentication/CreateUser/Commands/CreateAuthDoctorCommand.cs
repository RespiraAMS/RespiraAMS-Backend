using Domain.Enums;
using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Authentication.CreateUser.Commands;

/// <summary>
/// Creates a new AuthDoctor account (login + role) as the first step of the
/// CreateUser saga. Correlated to the saga via <see cref="SagaId"/>.
/// </summary>
public record CreateAuthDoctorCommand : ICommand
{
    public required Guid SagaId { get; init; }
    public required Guid AuthUserId { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string Phone { get; init; }
    public required RoleType Role { get; init; }
}
