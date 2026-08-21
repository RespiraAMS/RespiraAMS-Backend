using Domain.Enums;
using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Tokens.Commands.CreateToken
{
    /// <summary>
    /// Command to persist a hashed token (refresh, verification, ...) for a user
    /// </summary>
    public record CreateTokenCommand : ICommand
    {
        /// <summary>
        /// Raw token value, hashed before being saved to database
        /// </summary>
        public required string Token { get; init; }

        /// <summary>
        /// ID of the user the token belongs to
        /// </summary>
        public required Guid AuthUserId { get; init; }

        /// <summary>
        /// Type of the token (refresh, access, email verification, password reset)
        /// </summary>
        public TokenType TokenType { get; init; }

        /// <summary>
        /// Expiration date of the token. Null means the token never expires
        /// </summary>
        public DateTimeOffset? ExpirationDate { get; init; }
    }
}