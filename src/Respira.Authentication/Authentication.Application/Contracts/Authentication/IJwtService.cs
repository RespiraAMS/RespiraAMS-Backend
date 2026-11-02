using Respira.Authentication.Domain.Entities;
using Respira.Authentication.Domain.Enums;

namespace Respira.Authentication.Application.Constracts.Authentication
{
    public interface IJwtService
    {
        string GenerateAccessToken(Account account);
        Task<string> GenerateRefreshTokenAsync(Account account);
        Task<(string, RoleType)> ValidateAccessToken(string token);
        Task<(string, RoleType)> ValidateRefreshToken(string token);
    }
}
