using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Respira.Authentication.Application.Contracts.Authentication;
using Respira.Authentication.Application.Contracts.Cache;
using Respira.Authentication.Application.Features.Authentication.Commands.ResetPassword;
using Respira.Authentication.Domain.Entities;
using Respira.Authentication.Domain.Enums;
using Respira.Authentication.Infrastructure.Authentication;
using Respira.Authentication.Infrastructure.Persistence;
using Respira.ServiceDefaults.Contracts.Messages.Audit;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Authentication.Infrastructure.Test.Features.ResetPassword;

public sealed class ResetPasswordCommandHandlerTest
    : IClassFixture<PostgresFixture>,
        IAsyncLifetime
{
    private static readonly Guid BootstrapAdminId = Guid.Parse(
        "11111111-1111-1111-1111-111111111111"
    );

    private readonly DbContextOptions<AuthDbContext> _options;
    private readonly AuthDbContext _context;
    private readonly HashService _hashService = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly Mock<IMessageBus> _busMock = new();
    private readonly IOptions<JwtOption> _jwtOption = Options.Create(
        new JwtOption
        {
            Issuer = "Respira.Authentication",
            Audience = "Respira.Client",
            Secret = "0123456789abcdef0123456789abcdef",
            AccessTokenExpires = 60,
            RefreshTokenExpires = 7,
        }
    );

    public ResetPasswordCommandHandlerTest(PostgresFixture fixture)
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

    [Fact]
    public async Task HandleAsync_ThrowsArgumentNullException_WhenCommandIsNull()
    {
        var handler = CreateHandler();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            handler.HandleAsync(null!, TestContext.Current.CancellationToken)
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_ReturnsBadRequest_WhenTokenIsBlank(string token)
    {
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new ResetPasswordCommand
            {
                Token = token,
                NewPassword = "NewPass@123",
                ConfirmPassword = "NewPass@123",
            },
            TestContext.Current.CancellationToken
        );

        Assert.False(result.IsSuccess());
        Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ReturnsBadRequest_WhenConfirmationDoesNotMatch()
    {
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new ResetPasswordCommand
            {
                Token = "valid-token",
                NewPassword = "NewPass@123",
                ConfirmPassword = "Different@123",
            },
            TestContext.Current.CancellationToken
        );

        Assert.False(result.IsSuccess());
        Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ReturnsBusinessRuleViolation_WhenPasswordViolatesPolicy()
    {
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new ResetPasswordCommand
            {
                Token = "valid-token",
                NewPassword = "weak",
                ConfirmPassword = "weak",
            },
            TestContext.Current.CancellationToken
        );

        Assert.False(result.IsSuccess());
        Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ReturnsUnauthorized_WhenTokenIsUnknownOrExpired()
    {
        var account = await SeedAccountAsync("expired-reset@respira.ams");
        const string expiredRawToken = "expired-raw-token";
        _context.Tokens.Add(
            new Token
            {
                HashToken = _hashService.HashToken(expiredRawToken),
                AccountId = account.Id,
                TokenType = TokenType.PasswordResetToken,
                ExpirationDate = DateTimeOffset.UtcNow.AddMinutes(-5),
            }
        );
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler();

        var unknownResult = await handler.HandleAsync(
            new ResetPasswordCommand
            {
                Token = "unknown-token",
                NewPassword = "NewPass@123",
                ConfirmPassword = "NewPass@123",
            },
            TestContext.Current.CancellationToken
        );
        var expiredResult = await handler.HandleAsync(
            new ResetPasswordCommand
            {
                Token = expiredRawToken,
                NewPassword = "NewPass@123",
                ConfirmPassword = "NewPass@123",
            },
            TestContext.Current.CancellationToken
        );

        Assert.False(unknownResult.IsSuccess());
        Assert.Equal(ApplicationStatus.Unauthorized, unknownResult.StatusCode);
        Assert.False(expiredResult.IsSuccess());
        Assert.Equal(ApplicationStatus.Unauthorized, expiredResult.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ReturnsUnauthorized_WhenAccountIsInactive()
    {
        var account = await SeedAccountAsync("inactive-reset@respira.ams", StatusType.Inactive);
        const string rawToken = "inactive-account-token";
        _context.Tokens.Add(
            new Token
            {
                HashToken = _hashService.HashToken(rawToken),
                AccountId = account.Id,
                TokenType = TokenType.PasswordResetToken,
                ExpirationDate = DateTimeOffset.UtcNow.AddHours(1),
            }
        );
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new ResetPasswordCommand
            {
                Token = rawToken,
                NewPassword = "NewPass@123",
                ConfirmPassword = "NewPass@123",
            },
            TestContext.Current.CancellationToken
        );

        Assert.False(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_UpdatesPassword_RevokesRefreshTokens_ClearsCache_AndPublishesAudit()
    {
        var account = await SeedAccountAsync("doctor-reset@respira.ams");
        const string rawResetToken = "live-reset-token-123";
        var refreshTokenHash = _hashService.HashToken("live-refresh-token-123");

        _context.Tokens.AddRange(
            new Token
            {
                HashToken = _hashService.HashToken(rawResetToken),
                AccountId = account.Id,
                TokenType = TokenType.PasswordResetToken,
                ExpirationDate = DateTimeOffset.UtcNow.AddHours(1),
            },
            new Token
            {
                HashToken = refreshTokenHash,
                AccountId = account.Id,
                TokenType = TokenType.RefreshToken,
                ExpirationDate = DateTimeOffset.UtcNow.AddDays(7),
            }
        );
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new ResetPasswordCommand
            {
                Token = $"  {rawResetToken}  ",
                NewPassword = "NewSecure@2026",
                ConfirmPassword = "NewSecure@2026",
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.True(result.Data);

        await using var verifyContext = new AuthDbContext(_options);
        var updatedAccount = await verifyContext
            .Accounts.AsNoTracking()
            .SingleAsync(x => x.Id == account.Id, TestContext.Current.CancellationToken);
        Assert.True(_hashService.VerifyPassword("NewSecure@2026", updatedAccount.HashPassword));

        var remainingTokens = await verifyContext
            .Tokens.Where(x => x.AccountId == account.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Empty(remainingTokens);

        var blacklisted = await verifyContext
            .BlacklistTokens.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.HashToken == refreshTokenHash,
                TestContext.Current.CancellationToken
            );
        Assert.NotNull(blacklisted);
        Assert.Equal("Password reset", blacklisted!.Reason);

        _cacheServiceMock.Verify(
            x =>
                x.RemoveAsync(
                    "auth:account:email:doctor-reset@respira.ams",
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _cacheServiceMock.Verify(
            x => x.RemoveAsync($"auth:account:id:{account.Id}", It.IsAny<CancellationToken>()),
            Times.Once
        );
        _busMock.Verify(
            x =>
                x.PublishAsync(
                    It.Is<AuditEvent>(e =>
                        e.Action == "Authentication.PasswordReset"
                        && e.ActorAccountId == account.Id
                        && e.EntityId == account.Id
                    ),
                    It.IsAny<DeliveryOptions?>()
                ),
            Times.Once
        );
    }

    private ResetPasswordCommandHandler CreateHandler() =>
        new(
            _context,
            _hashService,
            _cacheServiceMock.Object,
            _jwtOption,
            NullLogger<ResetPasswordCommandHandler>.Instance,
            _busMock.Object
        );

    private async Task<Account> SeedAccountAsync(
        string email,
        StatusType status = StatusType.Active
    )
    {
        var account = new Account
        {
            Email = email,
            HashPassword = _hashService.HashPassword("OldPass@123"),
            Phone = "0901234567",
            Role = RoleType.Doctor,
            IsEmailConfirmed = true,
            Status = status,
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return account;
    }
}
