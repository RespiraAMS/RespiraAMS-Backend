using Application.Features.Authentication.SendEmailVerification;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Constracts.CQRS;
using Wolverine;

namespace Application.Features.Authentication.CreateUser.Events;

/// <summary>
/// Reacts to <see cref="AuthDoctorCreatedEvent"/> by dispatching the existing
/// email-verification request command. Uses <c>SendAsync</c> (fire-and-forget) so
/// account creation is not blocked on email delivery; the verification token is
/// generated and the link emailed by <see cref="RequestEmailVerificationCommandHandler"/>.
/// </summary>
/// <param name="logger">Logger.</param>
/// <param name="bus">Message bus used to dispatch the verification request.</param>
public class AuthDoctorCreatedEventHandler(
    ILogger<AuthDoctorCreatedEventHandler> logger,
    IMessageBus bus
) : ICommandHandler<AuthDoctorCreatedEvent>
{
    /// <summary>
    /// Sends the verification email request for the newly created account.
    /// </summary>
    /// <param name="message">Event carrying the new account's email.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task HandleAsync(
        AuthDoctorCreatedEvent message,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation(
            "Auth account {AuthUserId} created — sending verification email to {Email}",
            message.AuthUserId,
            message.Email
        );

        await bus.SendAsync(new RequestEmailVerificationCommand { Email = message.Email });
    }
}
