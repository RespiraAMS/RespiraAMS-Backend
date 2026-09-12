using Authentication.Application.Constracts.Authentication;
using Authentication.Domain.Entities;
using Authentication.Domain.Enums;

namespace Authentication.Infrastructure.Authentication
{
    public class JwtService : IJwtService
    {
        public string GenerateAccessToken(Account account)
        {
            throw new NotImplementedException();
        }

        public string GenerateRefreshToken(Account account)
        {
            throw new NotImplementedException();
        }

        public Task<(string, RoleType)> ValidateAccessToken(string token)
        {
            throw new NotImplementedException();
        }

        public Task<(string, RoleType)> ValidateRefreshToken(string token)
        {
            throw new NotImplementedException();
        }
    }
}
