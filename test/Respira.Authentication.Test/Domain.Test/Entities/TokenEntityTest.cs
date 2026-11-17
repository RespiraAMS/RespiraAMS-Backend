using Respira.Authentication.Domain.Entities;
using Respira.Authentication.Domain.Enums;
using Respira.ServiceDefaults.Models;

namespace Respira.Authentication.Domain.Test.Entities
{
    public class TokenEntityTest
    {
        [Fact]
        public void NewToken_HasNonEmptyUuidV7IdAndAuditDefaults()
        {
            var before = DateTimeOffset.UtcNow;

            var token = new Token
            {
                HashToken = "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855",
                AccountId = Guid.CreateVersion7(),
                TokenType = TokenType.RefreshToken,
            };

            var after = DateTimeOffset.UtcNow;
            Assert.NotEqual(Guid.Empty, token.Id);
            Assert.Equal(7, token.Id.Version);
            Assert.InRange(token.CreatedAt, before, after);
            Assert.InRange(token.UpdatedAt, before, after);
            Assert.False(token.IsDeleted);
            Assert.Null(token.DeletedAt);
            Assert.IsAssignableFrom<Base>(token);
        }

        [Fact]
        public void NewToken_NavigationAndExpirationDefaultToNull()
        {
            var token = new Token { HashToken = "HASH" };

            Assert.Null(token.Account);
            Assert.Null(token.ExpirationDate);
            Assert.Equal(Guid.Empty, token.AccountId);
            Assert.Equal(TokenType.RefreshToken, token.TokenType);
        }

        [Fact]
        public void IsExpired_ReturnsTrue_WhenExpirationDateIsInThePast()
        {
            var token = new Token
            {
                HashToken = "HASH",
                ExpirationDate = DateTimeOffset.UtcNow.AddMinutes(-5),
            };

            Assert.True(token.IsExpired());
        }

        [Fact]
        public void IsExpired_ReturnsFalse_WhenExpirationDateIsInTheFuture()
        {
            var token = new Token
            {
                HashToken = "HASH",
                ExpirationDate = DateTimeOffset.UtcNow.AddMinutes(5),
            };

            Assert.False(token.IsExpired());
        }

        [Fact]
        public void IsExpired_ReturnsFalse_WhenExpirationDateIsNull()
        {
            var token = new Token
            {
                HashToken = "HASH",
                ExpirationDate = null,
            };

            Assert.False(token.IsExpired());
        }

        [Fact]
        public void Token_CanReferenceOwningAccountAndAllProperties()
        {
            var account = new Account
            {
                Email = "doc@respira.ams",
                HashPassword = "hash",
                Phone = "0901112233",
                Role = RoleType.Doctor,
                Status = StatusType.Active,
            };
            var expires = DateTimeOffset.UtcNow.AddHours(1);

            var token = new Token
            {
                HashToken = "TOKEN_HASH",
                AccountId = account.Id,
                Account = account,
                TokenType = TokenType.EmailVerificationToken,
                ExpirationDate = expires,
            };

            Assert.Equal("TOKEN_HASH", token.HashToken);
            Assert.Equal(account.Id, token.AccountId);
            Assert.Same(account, token.Account);
            Assert.Equal(TokenType.EmailVerificationToken, token.TokenType);
            Assert.Equal(expires, token.ExpirationDate);
        }
    }
}
