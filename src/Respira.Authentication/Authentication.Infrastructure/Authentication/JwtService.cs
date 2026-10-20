using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Authentication.Application.Constracts.Authentication;
using Authentication.Application.Constracts.Data;
using Authentication.Domain.Entities;
using Authentication.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Authentication.Infrastructure.Authentication
{
    public class JwtService(
        IOptions<JwtOption> jwtOption,
        IAuthDbContext authDbContext,
        IHashService hashService
    ) : IJwtService
    {
        private readonly JwtOption _jwtOption = jwtOption.Value;
        private readonly IAuthDbContext _authDbContext = authDbContext;
        private readonly IHashService _hashService = hashService;

        public string GenerateAccessToken(Account account)
        {
            var expirationDate = DateTimeOffset.UtcNow.AddMinutes(_jwtOption.AccessTokenExpires);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Email, account.Email),
                new(ClaimTypes.NameIdentifier, account.Id.ToString()),
                new(ClaimTypes.Email, account.Email),
                new(ClaimTypes.Role, account.Role.ToString()),
            };

            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtOption.SecretKey)
            );
            var signingCredentials = new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256
            );

            var token = new JwtSecurityToken(
                issuer: _jwtOption.Issuer,
                audience: _jwtOption.Audience,
                claims: claims,
                expires: expirationDate.UtcDateTime,
                signingCredentials: signingCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<string> GenerateRefreshTokenAsync(Account account)
        {
            var expirationDate = DateTimeOffset.UtcNow.AddDays(_jwtOption.RefreshTokenExpires);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, account.Email),
                new(ClaimTypes.NameIdentifier, account.Id.ToString()),
                new(ClaimTypes.Email, account.Email),
                new(ClaimTypes.Role, account.Role.ToString()),
            };

            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtOption.SecretKey)
            );
            var signingCredentials = new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha512
            );

            var token = new JwtSecurityToken(
                issuer: _jwtOption.Issuer,
                audience: _jwtOption.Audience,
                claims: claims,
                expires: expirationDate.UtcDateTime,
                signingCredentials: signingCredentials
            );

            var rawToken = new JwtSecurityTokenHandler().WriteToken(token);

            var tokenEntity = new Token
            {
                HashToken = _hashService.HashToken(rawToken),
                AccountId = account.Id,
                TokenType = TokenType.RefreshToken,
                ExpirationDate = expirationDate,
            };

            _authDbContext.Tokens.Add(tokenEntity);
            await _authDbContext.SaveChangesAsync();

            return rawToken;
        }

        public Task<(string, RoleType)> ValidateAccessToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtOption.SecretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _jwtOption.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtOption.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);

            var accountId =
                principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new SecurityTokenException("Missing account ID claim.");

            var role =
                principal.FindFirstValue(ClaimTypes.Role)
                ?? throw new SecurityTokenException("Missing role claim.");

            var roleType = Enum.Parse<RoleType>(role);

            return Task.FromResult((accountId, roleType));
        }

        public async Task<(string, RoleType)> ValidateRefreshToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtOption.SecretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _jwtOption.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtOption.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);

            var accountId =
                principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new SecurityTokenException("Missing account ID claim.");

            var hashToken = _hashService.HashToken(token);

            var storedToken = await _authDbContext.Tokens.FirstOrDefaultAsync(t =>
                t.HashToken == hashToken
                && t.AccountId == Guid.Parse(accountId)
                && t.TokenType == TokenType.RefreshToken
            );

            if (storedToken?.IsExpired() != false)
                throw new SecurityTokenException("Refresh token is invalid or expired.");

            var account = await _authDbContext.Accounts.FirstOrDefaultAsync(a =>
                a.Id == storedToken.AccountId && !a.IsDeleted
            );

            if (account is null || account.Status != StatusType.Active)
                throw new SecurityTokenException("Account is inactive or no longer exists.");

            return (accountId, account.Role);
        }
    }
}
