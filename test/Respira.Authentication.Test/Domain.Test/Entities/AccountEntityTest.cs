using Respira.Authentication.Domain.Entities;
using Respira.Authentication.Domain.Enums;
using Respira.ServiceDefaults.Models;

namespace Respira.Authentication.Domain.Test.Entities
{
    public class AccountEntityTest
    {
        private static Account CreateAccount() =>
            new()
            {
                Email = "doctor@respira.ams",
                HashPassword = "$2a$12$hashedpassword",
                Phone = "0901234567",
                Role = RoleType.Doctor,
                IsEmailConfirmed = false,
                Status = StatusType.Active,
            };

        [Fact]
        public void NewAccount_HasANonEmptyUuidV7Identifier()
        {
            var account = CreateAccount();

            Assert.NotEqual(Guid.Empty, account.Id);
            Assert.Equal(7, account.Id.Version);
        }

        [Fact]
        public void NewAccount_HasCreatedAtAndUpdatedAtCloseToNow()
        {
            var before = DateTimeOffset.UtcNow;

            var account = CreateAccount();

            var after = DateTimeOffset.UtcNow;
            Assert.InRange(account.CreatedAt, before, after);
            Assert.InRange(account.UpdatedAt, before, after);
            Assert.Equal(TimeSpan.Zero, account.CreatedAt.Offset);
            Assert.Equal(TimeSpan.Zero, account.UpdatedAt.Offset);
        }

        [Fact]
        public void NewAccount_IsNotSoftDeletedAndHasNoDeletedAt()
        {
            var account = CreateAccount();

            Assert.False(account.IsDeleted);
            Assert.Null(account.DeletedAt);
        }

        [Fact]
        public void NewAccount_DerivesFromServiceDefaultsBase()
        {
            var account = CreateAccount();

            Assert.IsAssignableFrom<Base>(account);
            Assert.IsNotType<Base>(account);
        }

        [Fact]
        public void NewAccount_HasEmptyTokensCollectionByDefault()
        {
            var account = CreateAccount();

            Assert.NotNull(account.Tokens);
            Assert.Empty(account.Tokens);
        }

        [Fact]
        public void Account_PropertiesCanBeAssignedAndMutated()
        {
            var account = CreateAccount();

            account.Email = "head@respira.ams";
            account.HashPassword = "$2a$12$updatedpassword";
            account.Phone = "0987654321";
            account.Role = RoleType.HeadDepartmentDoctor;
            account.IsEmailConfirmed = true;
            account.Status = StatusType.Inactive;

            Assert.Equal("head@respira.ams", account.Email);
            Assert.Equal("$2a$12$updatedpassword", account.HashPassword);
            Assert.Equal("0987654321", account.Phone);
            Assert.Equal(RoleType.HeadDepartmentDoctor, account.Role);
            Assert.True(account.IsEmailConfirmed);
            Assert.Equal(StatusType.Inactive, account.Status);
        }

        [Fact]
        public void Account_TokensCollectionCanCarryIssuedTokens()
        {
            var account = CreateAccount();
            var token = new Token
            {
                HashToken = "ABC123DEF456",
                AccountId = account.Id,
                TokenType = TokenType.RefreshToken,
                ExpirationDate = DateTimeOffset.UtcNow.AddDays(7),
            };

            account.Tokens.Add(token);

            var single = Assert.Single(account.Tokens);
            Assert.Same(token, single);
        }

        [Fact]
        public void TwoAccountsCreatedBackToBack_GetDifferentIds()
        {
            var first = CreateAccount();
            var second = CreateAccount();

            Assert.NotEqual(first.Id, second.Id);
        }
    }
}
