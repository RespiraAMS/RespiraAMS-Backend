using Application.Abstracts.Data;
using Application.Features.Tokens.Commands.CreateToken;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Dtos;
using Respira.ServiceDefaults.Exceptions;
using Wolverine;

namespace Application.Features.Authentication.SendEmailVerification;

/// <summary>
/// Handles a request to (re)send the email verification link: looks up the doctor,
/// generates and persists a fresh verification token, then emails the link.
/// </summary>
/// <param name="dbContext">Auth database context</param>
/// <param name="messageBus">Message bus used to persist and send the token</param>
/// <param name="logger">Logger</param>
public class RequestEmailVerificationCommandHandler(
    IAuthDbContext dbContext,
    IMessageBus messageBus,
    ILogger<RequestEmailVerificationCommandHandler> logger
) : ICommandHandler<RequestEmailVerificationCommand, ApiResponse<bool>>
{
    /// <summary>
    /// Generates a verification token, persists it and sends the verification email.
    /// Idempotent for already-confirmed accounts and fails gracefully for unknown emails.
    /// </summary>
    /// <param name="command">Request holding the target email</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response, or a failure response on invalid request</returns>
    public async Task<ApiResponse<bool>> HandleAsync(
        RequestEmailVerificationCommand command,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var email = command.Email.Trim().ToLower();
            var doctor = await dbContext.AuthDoctors.FirstOrDefaultAsync(
                x => x.Email.ToLower() == email,
                cancellationToken
            );

            if (doctor is null)
            {
                return ApiResponse<bool>.Fail(
                    message: "Invalid request",
                    statusCode: StatusCodes.Status400BadRequest
                );
            }

            // Already confirmed: nothing to do.
            if (doctor.IsEmailConfirmed)
            {
                return ApiResponse<bool>.Ok(true);
            }

            var token = GenerateVerificationToken();
            var saved = await messageBus.InvokeAsync<bool>(
                new CreateTokenCommand
                {
                    AuthUserId = doctor.Id,
                    Token = token,
                    TokenType = TokenType.EmailVerificationToken,
                    ExpirationDate = DateTimeOffset.UtcNow.AddHours(24),
                }
            );

            if (!saved)
            {
                throw new ServerException();
            }

            var sent = await messageBus.InvokeAsync<bool>(
                new SendEmaiLVerificationCommand { Email = email, Token = token }
            );

            if (!sent)
            {
                throw new ServerException();
            }

            return ApiResponse<bool>.Ok(true);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to send verification email to {Email}", command.Email);
            throw new ServerException(e);
        }
    }

    /// <summary>
    /// Generates a URL-safe random verification token.
    /// </summary>
    private static string GenerateVerificationToken()
    {
        var bytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert
            .ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
}
