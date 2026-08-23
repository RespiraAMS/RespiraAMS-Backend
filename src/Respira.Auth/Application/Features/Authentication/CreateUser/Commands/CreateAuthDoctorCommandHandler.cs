using Application.Abstracts.Authentication;
using Application.Abstracts.Data;
using Application.Features.Authentication.CreateUser.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Constracts.CQRS;
using Wolverine;

namespace Application.Features.Authentication.CreateUser.Commands;

/// <summary>
/// Handles <see cref="CreateAuthDoctorCommand"/>: creates the auth doctor account
/// (hashed password, default role/status) and emits <c>CreateAuthDoctorSuccess</c> or
/// <c>CreateAuthDoctorFailure</c> to drive the CreateUser saga forward or compensate.
/// </summary>
public class CreateAuthDoctorCommandHandler(
    ILogger<CreateAuthDoctorCommand> logger,
    IAuthDbContext dbContext,
    IHashService hashService,
    IMessageBus bus
) : ICommandHandler<CreateAuthDoctorCommand>
{
    /// <summary>
    /// Creates the auth doctor account and publishes the corresponding saga outcome event.
    /// </summary>
    /// <param name="command">Create auth doctor command with the new account details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task HandleAsync(
        CreateAuthDoctorCommand command,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var email = command.Email.ToLowerInvariant();
            var alreadyExists = await dbContext.AuthDoctors.AnyAsync(
                x => x.Email == email,
                cancellationToken
            );
            if (alreadyExists)
            {
                logger.LogWarning("Auth user with email {Email} already exists", email);
                await bus.PublishAsync(
                    new CreateAuthDoctorFailure
                    {
                        SagaId = command.SagaId,
                        Message = "Email already registered",
                    }
                );
                return;
            }

            var authDoctor = new Domain.Entities.AuthDoctor
            {
                Id = command.AuthUserId,
                Email = email,
                HashPassword = hashService.HashPassword(command.Password),
                Phone = command.Phone,
                Role = command.Role,
                IsEmailConfirmed = false,
                Status = Domain.Enums.StatusType.Active,
            };

            dbContext.AuthDoctors.Add(authDoctor);
            await dbContext.SaveChangesAsync();

            await bus.PublishAsync(
                new CreateAuthDoctorSuccess
                {
                    SagaId = command.SagaId,
                    AuthUserId = authDoctor.Id,
                }
            );
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to create auth doctor");
            await bus.PublishAsync(
                new CreateAuthDoctorFailure
                {
                    SagaId = command.SagaId,
                    Message = "Failed to create user account",
                }
            );
        }
    }
}
