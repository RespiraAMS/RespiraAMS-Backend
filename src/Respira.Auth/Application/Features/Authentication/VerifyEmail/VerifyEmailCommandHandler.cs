using Application.Abstracts.Authentication;
using Application.Abstracts.Data;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Dtos;
using Respira.ServiceDefaults.Exceptions;

namespace Application.Features.Authentication.VerifyEmail
{
    /// <summary>
    /// Confirms a doctor's email using the verification token sent by email.
    /// </summary>
    /// <param name="dbContext">Auth database context</param>
    /// <param name="logger">Logger</param>
    /// <param name="hashService">Token hashing utility</param>
    public class VerifyEmailCommandHandler(
        IAuthDbContext dbContext,
        ILogger<VerifyEmailCommand> logger,
        IHashService hashService
    ) : ICommandHandler<VerifyEmailCommand, Result<bool>>
    {
        /// <summary>
        /// Validates the verification token, marks the account email as confirmed and
        /// removes the consumed token. Idempotent if already confirmed.
        /// </summary>
        /// <param name="command">Verification token and email</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success response, or a failure response on invalid/expired token</returns>
        public async Task<Result<bool>> HandleAsync(
            VerifyEmailCommand command,
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
                    return Result<bool>.Fail(
                        message: "Invalid verification request",
                        statusCode: StatusCodes.Status400BadRequest
                    );
                }

                // Already confirmed: idempotent success
                if (doctor.IsEmailConfirmed)
                {
                    return Result<bool>.Ok(true);
                }

                var hashToken = hashService.HashToken(command.Token);
                var token = await dbContext.Tokens.FirstOrDefaultAsync(
                    x =>
                        x.HashToken == hashToken
                        && x.TokenType == TokenType.EmailVerificationToken
                        && x.AuthUserId == doctor.Id,
                    cancellationToken
                );

                if (token?.IsExpired() != false)
                {
                    return Result<bool>.Fail(
                        message: "Invalid or expired verification token",
                        statusCode: StatusCodes.Status400BadRequest
                    );
                }

                doctor.IsEmailConfirmed = true;
                dbContext.Tokens.Remove(token);

                if (await dbContext.SaveChangesAsync() <= 0)
                {
                    throw new ServerException();
                }

                return Result<bool>.Ok(true);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to verify email");
                throw new ServerException(e);
            }
        }
    }
}
