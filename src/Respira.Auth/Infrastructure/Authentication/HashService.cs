using System.Security.Cryptography;
using System.Text;
using Application.Abstracts.Authentication;

namespace Infrastructure.Authentication
{
    /// <summary>
    /// Hashing utilities: BCrypt for passwords, SHA-256 for tokens
    /// </summary>
    public class HashService : IHashService
    {
        private const int WorkFactor = 12;

        /// <summary>
        /// Hashes a password using BCrypt
        /// </summary>
        /// <param name="password">Password to hash</param>
        /// <returns>BCrypt hash string</returns>
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: WorkFactor);
        }

        /// <summary>
        /// Verifies a password against a BCrypt hash
        /// </summary>
        /// <param name="password">Plain password to verify</param>
        /// <param name="hashedPassword">BCrypt hash to compare against</param>
        /// <returns>True if the password matches the hash</returns>
        public bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }

        /// <summary>
        /// Hashes a (high-entropy) token using SHA-256
        /// </summary>
        /// <param name="token">Token to hash</param>
        /// <returns>Hex-encoded SHA-256 hash</returns>
        public string HashToken(string token)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        }

        /// <summary>
        /// Verifies a token against a SHA-256 hash using constant-time comparison
        /// </summary>
        /// <param name="token">Plain token to verify</param>
        /// <param name="hashedToken">Hex-encoded SHA-256 hash to compare against</param>
        /// <returns>True if the token matches the hash</returns>
        public bool VerifyToken(string token, string hashedToken)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            var expected = Convert.FromHexString(hashedToken);
            return hash.Length == expected.Length
                && CryptographicOperations.FixedTimeEquals(hash, expected);
        }
    }
}