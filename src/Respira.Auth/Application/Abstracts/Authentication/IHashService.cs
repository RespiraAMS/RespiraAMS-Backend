namespace Application.Abstracts.Authentication
{
    /// <summary>
    /// Hashing utilities used to protect passwords and tokens.
    /// BCrypt is used for passwords (slow, salted); SHA-256 for tokens (fast, high-entropy).
    /// </summary>
    public interface IHashService
    {
        /// <summary>Hashes a password using BCrypt.</summary>
        /// <param name="password">Plain-text password to hash.</param>
        /// <returns>The BCrypt hash string.</returns>
        public string HashPassword(string password);

        /// <summary>Verifies a plain-text password against a BCrypt hash.</summary>
        /// <param name="password">Plain-text password to verify.</param>
        /// <param name="hashedPassword">BCrypt hash to compare against.</param>
        /// <returns>True if the password matches the hash.</returns>
        public bool VerifyPassword(string password, string hashedPassword);

        /// <summary>Hashes a (high-entropy) token using SHA-256.</summary>
        /// <param name="token">Raw token to hash.</param>
        /// <returns>Hex-encoded SHA-256 hash.</returns>
        public string HashToken(string token);

        /// <summary>Verifies a token against a SHA-256 hash using constant-time comparison.</summary>
        /// <param name="token">Plain token to verify.</param>
        /// <param name="hashedToken">Hex-encoded SHA-256 hash to compare against.</param>
        /// <returns>True if the token matches the hash.</returns>
        public bool VerifyToken(string token, string hashedToken);
    }
}
