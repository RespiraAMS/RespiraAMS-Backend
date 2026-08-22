using Application.Abstracts.Authentication;
using Application.Abstracts.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Tokens.Commands.CreateToken
{
    /// <summary>
    /// Persists a token (hashed) for a user in the database
    /// </summary>
    /// <param name="logger">Logger</param>
    /// <param name="dbContext">Auth database context</param>
    /// <param name="hashService">Token hashing utility</param>
    public class CreateTokenHandler(
        ILogger<CreateTokenHandler> logger,
        IAuthDbContext dbContext,
        IHashService hashService
    ) : ICommandHandler<CreateTokenCommand, bool>
    {
        /// <summary>
        /// Saves the token hashed with SHA-256 if the user exists
        /// </summary>
        /// <param name="command">Token to persist</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the token was saved, false if the user does not exist or saving failed</returns>
        public async Task<bool> HandleAsync(
            CreateTokenCommand command,
            CancellationToken cancellationToken = default
        )
        {
            var exists = await dbContext.AuthDoctors.AnyAsync(
                x => x.Id == command.AuthUserId,
                cancellationToken
            );
            if (!exists)
            {
                logger.LogDebug("Token creation failed: user {AuthUserId} not found", command.AuthUserId);
                return false;
            }

            var token = new Token
            {
                HashToken = hashService.HashToken(command.Token),
                AuthUserId = command.AuthUserId,
                TokenType = command.TokenType,
                ExpirationDate = command.ExpirationDate,
            };

            await dbContext.Tokens.AddAsync(token, cancellationToken);
            if (await dbContext.SaveChangesAsync() <= 0)
            {
                logger.LogError("Failed to save token for user {AuthUserId}", command.AuthUserId);
                return false;
            }

            return true;
        }
    }
}