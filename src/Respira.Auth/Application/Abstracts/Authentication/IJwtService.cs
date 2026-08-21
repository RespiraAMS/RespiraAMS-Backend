using Domain.Enums;

namespace Application.Abstracts.Authentication
{
    /// <summary>
    /// Generates and validates JWT access and refresh tokens.
    /// </summary>
    public interface IJwtService
    {
        /// <summary>Generates a signed JWT access token for the given user</summary>
        /// <param name="email">User email (used as the subject claim)</param>
        /// <param name="role">User role (used as the role claim)</param>
        /// <returns>Signed JWT access token</returns>
        public string GenerateToken(string email, RoleType role);

        /// <summary>Validates an access token and returns the user's email and role</summary>
        /// <param name="token">Access token to validate</param>
        /// <returns>Tuple of (email, role)</returns>
        public Task<(string, RoleType)> ValidateToken(string token);

        /// <summary>Generates a signed JWT refresh token for the given user</summary>
        /// <param name="email">User email (used as the subject claim)</param>
        /// <param name="role">User role (used as the role claim)</param>
        /// <returns>Signed JWT refresh token</returns>
        public string GenerateRefreshToken(string email, RoleType role);

        /// <summary>Validates a refresh token and returns the user's email and role</summary>
        /// <param name="token">Refresh token to validate</param>
        /// <returns>Tuple of (email, role)</returns>
        public Task<(string, RoleType)> ValidateRefreshToken(string token);
    }
}
