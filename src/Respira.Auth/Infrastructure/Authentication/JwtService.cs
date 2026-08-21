using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Abstracts.Authentication;
using Application.Abstracts.Data;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Authentication
{
    /// <summary>
    /// Generates and validates JWT access and refresh tokens (HMAC-SHA256).
    /// Access tokens are additionally rejected when present in the blacklist.
    /// </summary>
    /// <param name="jwtOption">JWT configuration (secret, issuer, audience, expiry)</param>
    /// <param name="hashService">Hashing utility used to compare the token against its stored hash</param>
    /// <param name="dbContext">Auth database context used to query the blacklist</param>
    public class JwtService(
        IOptions<JwtOption> jwtOption,
        IHashService hashService,
        IAuthDbContext dbContext
    ) : IJwtService
    {
        private const string TokenTypeClaim = "token_type";

        /// <summary>
        /// Generates an access token for the given user
        /// </summary>
        /// <param name="email">Email of the user</param>
        /// <param name="role">Role of the user</param>
        /// <returns>Signed JWT access token</returns>
        public string GenerateToken(string email, RoleType role)
        {
            return GenerateToken(email, role, TokenType.AccessToken, jwtOption.Value.AccessTokenExpired);
        }

        /// <summary>
        /// Generates a refresh token for the given user
        /// </summary>
        /// <param name="email">Email of the user</param>
        /// <param name="role">Role of the user</param>
        /// <returns>Signed JWT refresh token</returns>
        public string GenerateRefreshToken(string email, RoleType role)
        {
            return GenerateToken(email, role, TokenType.RefreshToken, jwtOption.Value.RefreshTokenExpired);
        }

        /// <summary>
        /// Validates an access token and returns the user email and role
        /// </summary>
        /// <param name="token">Access token to validate</param>
        /// <returns>User email and role</returns>
        /// <exception cref="UnauthorizedAccessException">Token is invalid, expired, blacklisted or not an access token</exception>
        public async Task<(string, RoleType)> ValidateToken(string token)
        {
            return await ValidateToken(token, TokenType.AccessToken);
        }

        /// <summary>
        /// Validates a refresh token and returns the user email and role
        /// </summary>
        /// <param name="token">Refresh token to validate</param>
        /// <returns>User email and role</returns>
        /// <exception cref="UnauthorizedAccessException">Token is invalid, expired, or not a refresh token</exception>
        public async Task<(string, RoleType)> ValidateRefreshToken(string token)
        {
            return await ValidateToken(token, TokenType.RefreshToken);
        }

        private string GenerateToken(string email, RoleType role, TokenType tokenType, int expiredInMinutes)
        {
            var option = jwtOption.Value;
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Email, email),
                new(ClaimTypes.Role, role.ToString()),
                new(TokenTypeClaim, tokenType.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(option.Secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var now = DateTime.UtcNow;

            var token = new JwtSecurityToken(
                issuer: option.Issuer,
                audience: option.Audience,
                claims: claims,
                notBefore: now,
                expires: now.AddMinutes(expiredInMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<(string, RoleType)> ValidateToken(string token, TokenType expectedType)
        {
            var option = jwtOption.Value;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(option.Secret));

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var principal = handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = option.Issuer,
                    ValidateAudience = true,
                    ValidAudience = option.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                }, out _);

                if (principal.FindFirstValue(TokenTypeClaim) != expectedType.ToString())
                {
                    throw new UnauthorizedAccessException();
                }

                // Reject access tokens that have been revoked (added to the blacklist).
                // Refresh tokens are revoked by removing their persisted row, so no
                // blacklist check is required for them here.
                if (expectedType == TokenType.AccessToken)
                {
                    var hashToken = hashService.HashToken(token);
                    var blacklisted = await dbContext.BlacklistTokens
                        .AnyAsync(b => b.HashToken == hashToken);
                    if (blacklisted)
                    {
                        throw new UnauthorizedAccessException();
                    }
                }

                var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email);
                var role = principal.FindFirstValue(ClaimTypes.Role);
                if (email is null
                    || role is null
                    || !Enum.TryParse<RoleType>(role, out var roleType))
                {
                    throw new UnauthorizedAccessException();
                }

                return (email, roleType);
            }
            catch (SecurityTokenException)
            {
                throw new UnauthorizedAccessException();
            }
        }
    }
}
