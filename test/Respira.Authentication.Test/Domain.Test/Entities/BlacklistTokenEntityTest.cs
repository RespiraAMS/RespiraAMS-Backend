using Respira.Authentication.Domain.Entities;
using Respira.ServiceDefaults.Models;

namespace Respira.Authentication.Domain.Test.Entities
{
    public class BlacklistTokenEntityTest
    {
        [Fact]
        public void NewBlacklistToken_HasUuidV7IdAndBaseDefaults()
        {
            var before = DateTimeOffset.UtcNow;

            var blacklisted = new BlacklistToken
            {
                HashToken = "REVOKED_HASH",
                Reason = "logout",
            };

            var after = DateTimeOffset.UtcNow;
            Assert.NotEqual(Guid.Empty, blacklisted.Id);
            Assert.Equal(7, blacklisted.Id.Version);
            Assert.InRange(blacklisted.CreatedAt, before, after);
            Assert.InRange(blacklisted.UpdatedAt, before, after);
            Assert.False(blacklisted.IsDeleted);
            Assert.Null(blacklisted.DeletedAt);
            Assert.IsAssignableFrom<Base>(blacklisted);
        }

        [Fact]
        public void NewBlacklistToken_ExpirationDateDefaultsToNull()
        {
            var blacklisted = new BlacklistToken
            {
                HashToken = "REVOKED_HASH",
                Reason = "password_reset",
            };

            Assert.Null(blacklisted.ExpirationDate);
            Assert.Equal("REVOKED_HASH", blacklisted.HashToken);
            Assert.Equal("password_reset", blacklisted.Reason);
        }

        [Fact]
        public void BlacklistToken_CarriesExpirationDateWhenProvided()
        {
            var expiration = new DateTimeOffset(2026, 12, 31, 23, 59, 59, TimeSpan.Zero);

            var blacklisted = new BlacklistToken
            {
                HashToken = "REVOKED_HASH",
                Reason = "logout",
                ExpirationDate = expiration,
            };

            Assert.Equal(expiration, blacklisted.ExpirationDate);
        }
    }
}
