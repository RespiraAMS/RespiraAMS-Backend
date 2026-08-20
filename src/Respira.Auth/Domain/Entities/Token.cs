using Domain.Enums;
using Respira.ServiceDefaults.Models;

namespace Domain.Entities
{
    /// <summary>
    /// A persisted token (refresh, email verification, password reset) issued to a user.
    /// Only the SHA-256 hash is stored; the raw token is never persisted.
    /// </summary>
    public class Token : Base
    {
        /// <summary>SHA-256 hash of the raw token</summary>
        public required string HashToken { get; init; }

        /// <summary>ID of the owning account</summary>
        public Guid AuthUserId { get; init; }

        /// <summary>Navigation to the owning account</summary>
        public AuthDoctor? AuthDoctor { get; init; }

        /// <summary>Type of the token</summary>
        public TokenType TokenType { get; init; }

        /// <summary>Expiration date of the token (null = never expires)</summary>
        public DateTimeOffset? ExpirationDate { get; init; }

        /// <summary>Returns true if the token is past its expiration date</summary>
        public bool IsExpired()
        {
            return ExpirationDate < DateTimeOffset.UtcNow;
        }
    }
}
