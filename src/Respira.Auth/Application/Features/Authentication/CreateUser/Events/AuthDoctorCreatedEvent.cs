using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Authentication.CreateUser.Events;

/// <summary>
/// Emitted once an auth doctor account has been persisted, to trigger the
/// out-of-band side effect of sending the email verification link. Consumed by
/// <see cref="Application.Features.Authentication.SendEmailVerification.AuthDoctorCreatedEventHandler"/>.
/// </summary>
public record AuthDoctorCreatedEvent : ICommand
{
    /// <summary>Identifier of the created auth doctor account.</summary>
    public required Guid AuthUserId { get; init; }

    /// <summary>Email of the created account (verification link recipient).</summary>
    public required string Email { get; init; }
}
