using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respira.Authentication.Domain.Entities;
using Respira.Authentication.Domain.Enums;
using Respira.Authentication.Infrastructure.Persistence;

namespace Respira.Authentication.Infrastructure.Test.Persistence;

public sealed class AuthDbContextTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private static readonly Guid BootstrapAdminId = Guid.Parse(
        "11111111-1111-1111-1111-111111111111"
    );

    private readonly DbContextOptions<AuthDbContext> _options;
    private readonly AuthDbContext _context;

    public AuthDbContextTest(PostgresFixture fixture)
    {
        _options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
        _context = new AuthDbContext(_options);
    }

    public async ValueTask InitializeAsync()
    {
        await _context
            .Tokens.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context
            .BlacklistTokens.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context
            .Accounts.IgnoreQueryFilters()
            .Where(x => x.Id != BootstrapAdminId)
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    #region Seeded bootstrap admin

    [Fact]
    public async Task Migrations_SeedBootstrapAdminAccount()
    {
        await using var context = new AuthDbContext(_options);
        var admin = await context
            .Accounts.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == BootstrapAdminId,
                TestContext.Current.CancellationToken
            );

        Assert.NotNull(admin);
        Assert.Equal("admin@respira.ams", admin!.Email);
        Assert.Equal(RoleType.Admin, admin.Role);
        Assert.Equal(StatusType.Active, admin.Status);
        Assert.True(admin.IsEmailConfirmed);
        Assert.False(admin.IsDeleted);
    }

    #endregion

    #region Global query filters

    [Fact]
    public async Task SoftDeletedAccount_IsHiddenFromPlainQueries_AndVisibleWithIgnoreQueryFilters()
    {
        var account = await SeedAccountAsync(isDeleted: true);

        await using var context = new AuthDbContext(_options);
        var visible = await context
            .Accounts.Where(x => x.Id == account.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
        var all = await context
            .Accounts.IgnoreQueryFilters()
            .Where(x => x.Id == account.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Empty(visible);
        Assert.Single(all);
        Assert.True(all[0].IsDeleted);
    }

    [Fact]
    public async Task SoftDeletedToken_IsHiddenFromPlainQueries_AndVisibleWithIgnoreQueryFilters()
    {
        var account = await SeedAccountAsync();
        var token = new Token
        {
            HashToken = $"HASH_{Guid.CreateVersion7():N}",
            AccountId = account.Id,
            TokenType = TokenType.RefreshToken,
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(7),
            IsDeleted = true,
            DeletedAt = DateTimeOffset.UtcNow,
        };
        _context.Tokens.Add(token);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var context = new AuthDbContext(_options);
        var visible = await context
            .Tokens.Where(x => x.Id == token.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
        var all = await context
            .Tokens.IgnoreQueryFilters()
            .Where(x => x.Id == token.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Empty(visible);
        Assert.Single(all);
    }

    [Fact]
    public async Task BlacklistToken_HasNoSoftDeleteFilter_RemainsVisibleEvenIfIsDeletedTrue()
    {
        var blacklisted = new BlacklistToken
        {
            HashToken = $"BLACKLIST_{Guid.CreateVersion7():N}",
            Reason = "logout",
            IsDeleted = true,
            DeletedAt = DateTimeOffset.UtcNow,
        };
        _context.BlacklistTokens.Add(blacklisted);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var context = new AuthDbContext(_options);
        var found = await context
            .BlacklistTokens.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == blacklisted.Id,
                TestContext.Current.CancellationToken
            );

        Assert.NotNull(found);
        Assert.Equal("logout", found!.Reason);
    }

    #endregion

    #region DateTimeOffset normalization

    [Fact]
    public async Task SaveChangesAsync_NormalizesNonUtcDateTimeOffsetsToUtc()
    {
        var offsetTime = new DateTimeOffset(2026, 6, 15, 14, 30, 0, TimeSpan.FromHours(7));
        var account = CreateAccount();
        account.CreatedAt = offsetTime;
        account.UpdatedAt = offsetTime;

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(TimeSpan.Zero, account.CreatedAt.Offset);
        Assert.Equal(offsetTime.UtcDateTime, account.CreatedAt.UtcDateTime);

        await using var verifyContext = new AuthDbContext(_options);
        var persisted = await verifyContext
            .Accounts.AsNoTracking()
            .SingleAsync(x => x.Id == account.Id, TestContext.Current.CancellationToken);

        Assert.Equal(TimeSpan.Zero, persisted.CreatedAt.Offset);
        Assert.Equal(offsetTime.UtcDateTime, persisted.CreatedAt.UtcDateTime);
    }

    #endregion

    #region Unique constraints and cascade delete

    [Fact]
    public async Task SavingTwoAccountsWithSameEmail_ThrowsUniqueViolation()
    {
        var email = $"duplicate-{Guid.CreateVersion7():N}@respira.ams";
        await SeedAccountAsync(email: email);

        await using var context = new AuthDbContext(_options);
        context.Accounts.Add(CreateAccount(email: email));

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() =>
            context.SaveChangesAsync(TestContext.Current.CancellationToken)
        );
        var pg = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, pg.SqlState);
    }

    [Fact]
    public async Task SavingTwoTokensWithSameHashToken_ThrowsUniqueViolation()
    {
        var account = await SeedAccountAsync();
        var duplicateHash = $"DUP_HASH_{Guid.CreateVersion7():N}";

        _context.Tokens.Add(
            new Token
            {
                HashToken = duplicateHash,
                AccountId = account.Id,
                TokenType = TokenType.RefreshToken,
            }
        );
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var secondContext = new AuthDbContext(_options);
        secondContext.Tokens.Add(
            new Token
            {
                HashToken = duplicateHash,
                AccountId = account.Id,
                TokenType = TokenType.EmailVerificationToken,
            }
        );

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() =>
            secondContext.SaveChangesAsync(TestContext.Current.CancellationToken)
        );
        var pg = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, pg.SqlState);
    }

    [Fact]
    public async Task SavingTwoBlacklistTokensWithSameHashToken_ThrowsUniqueViolation()
    {
        var duplicateHash = $"DUP_BLACKLIST_{Guid.CreateVersion7():N}";

        _context.BlacklistTokens.Add(
            new BlacklistToken { HashToken = duplicateHash, Reason = "logout" }
        );
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var secondContext = new AuthDbContext(_options);
        secondContext.BlacklistTokens.Add(
            new BlacklistToken { HashToken = duplicateHash, Reason = "compromised" }
        );

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() =>
            secondContext.SaveChangesAsync(TestContext.Current.CancellationToken)
        );
        var pg = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, pg.SqlState);
    }

    [Fact]
    public async Task DeletingAccount_CascadeDeletesOwnedTokens()
    {
        var account = await SeedAccountAsync();
        var token = new Token
        {
            HashToken = $"CASCADE_{Guid.CreateVersion7():N}",
            AccountId = account.Id,
            TokenType = TokenType.RefreshToken,
        };
        _context.Tokens.Add(token);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using (var deleteContext = new AuthDbContext(_options))
        {
            var tracked = await deleteContext.Accounts.SingleAsync(
                x => x.Id == account.Id,
                TestContext.Current.CancellationToken
            );
            deleteContext.Accounts.Remove(tracked);
            await deleteContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verifyContext = new AuthDbContext(_options);
        var remainingTokens = await verifyContext
            .Tokens.IgnoreQueryFilters()
            .Where(x => x.AccountId == account.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Empty(remainingTokens);
    }

    #endregion

    #region Helpers

    private static Account CreateAccount(
        string? email = null,
        RoleType role = RoleType.Doctor,
        StatusType status = StatusType.Active
    ) =>
        new()
        {
            Email = email ?? $"doctor-{Guid.CreateVersion7():N}@respira.ams",
            HashPassword = "$2a$12$hashedpassword",
            Phone = "0901234567",
            Role = role,
            IsEmailConfirmed = true,
            Status = status,
        };

    private async Task<Account> SeedAccountAsync(
        string? email = null,
        RoleType role = RoleType.Doctor,
        StatusType status = StatusType.Active,
        bool isDeleted = false
    )
    {
        var account = CreateAccount(email, role, status);
        if (isDeleted)
        {
            account.IsDeleted = true;
            account.DeletedAt = DateTimeOffset.UtcNow;
        }

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return account;
    }

    #endregion
}
