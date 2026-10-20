using System.Security.Cryptography;
using System.Text;
using Authentication.Application.Constracts.Authentication;

namespace Authentication.Infrastructure.Authentication
{
    public class HashService : IHashService
    {
        public string HashPassword(string password)
        {
            ArgumentException.ThrowIfNullOrEmpty(password);
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public string HashToken(string token)
        {
            ArgumentException.ThrowIfNullOrEmpty(token);
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        }

        public bool VerifyPassword(string password, string hash)
        {
            ArgumentException.ThrowIfNullOrEmpty(password);
            ArgumentException.ThrowIfNullOrEmpty(hash);
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                return false;
            }
        }

        public bool VerifyToken(string token, string hash)
        {
            ArgumentException.ThrowIfNullOrEmpty(token);
            ArgumentException.ThrowIfNullOrEmpty(hash);

            try
            {
                var expectedHash = Convert.FromHexString(HashToken(token));
                var actualHash = Convert.FromHexString(hash);
                return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
