using Respira.ServiceDefaults.Models;

namespace Domain.Entities
{
    /// <summary>
    /// A revoked token that must be rejected even if it has not expired yet.
    /// Entries are permanent (no soft-delete) so revocation survives token expiry.
    /// </summary>
    public class BlacklistToken : Base
    {
        /// <summary>SHA-256 hash of the revoked token</summary>
        public required string HashToken { get; init; }

        /// <summary>Reason the token was revoked (e.g. "logout", "password_reset")</summary>
        public required string Reason { get; init; } = string.Empty;

        /// <summary>Original expiration of the revoked token (used for cleanup)</summary>
        public DateTimeOffset? ExpirationDate { get; init; }
    }
}
