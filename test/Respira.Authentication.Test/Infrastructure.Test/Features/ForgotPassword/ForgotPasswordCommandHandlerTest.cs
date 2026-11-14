using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Respira.Authentication.Application.Contracts.Authentication;
using Respira.Authentication.Application.Contracts.Cache;
using Respira.Authentication.Application.Contracts.Email;
using Respira.Authentication.Application.Features.Authentication.Commands.ForgotPassword;
using Respira.Authentication.Domain.Entities;
using Respira.Authentication.Domain.Enums;
using Respira.Authentication.Infrastructure.Authentication;
using Respira.Authentication.Infrastructure.Persistence;
using Respira.ServiceDefaults.Contracts.Messages.Audit;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Authentication.Infrastructure.Test.Features.ForgotPassword;

public sealed class ForgotPasswordCommandHandlerTest
    : IClassFixture<PostgresFixture>,
        IAsyncLifetime
{
    private static readonly Guid BootstrapAdminId = Guid.Parse(
        "11111111-1111-1111-1111-111111111111"
    );

    private readonly DbContextOptions<AuthDbContext> _options;
    private readonly AuthDbContext _context;
    private readonly HashService _hashService = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly Mock<IMessageBus> _busMock = new();
    private readonly IOptions<PasswordResetOption> _resetOption = Options.Create(
        new PasswordResetOption
        {
            ResetUrl = "https://respira.ams/reset-password",
            TokenExpiresHours = 2,
        }
    );

    public ForgotPasswordCommandHandlerTest(PostgresFixture fixture)
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
    public async Task HandleAsync_ReturnsBadRequest_AndPublishesFailedAudit_WhenEmailIsBlank(
        string email
    )
    {
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new ForgotPasswordCommand { Email = email },
            TestContext.Current.CancellationToken
        );

        Assert.False(result.IsSuccess());
        Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
        _busMock.Verify(
            x =>
                x.PublishAsync(
                    It.Is<AuditEvent>(e => e.Action == "Authentication.PasswordResetEmailFailed"),
                    It.IsAny<DeliveryOptions?>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleAsync_ReturnsBusinessRuleViolation_WhenCooldownKeyExists()
    {
        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<string>(
                    "auth:account:reset:cooldown:doctor@respira.ams",
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync("1");

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new ForgotPasswordCommand { Email = "  Doctor@Respira.Ams " },
            TestContext.Current.CancellationToken
        );

        Assert.False(result.IsSuccess());
        Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.StatusCode);
        _emailServiceMock.Verify(
            x =>
                x.SendAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task HandleAsync_ReturnsBadRequest_WhenAccountDoesNotExistOrIsInactive()
    {
        await SeedAccountAsync("inactive@respira.ams", StatusType.Inactive);

        var handler = CreateHandler();

        var missingResult = await handler.HandleAsync(
            new ForgotPasswordCommand { Email = "unknown@respira.ams" },
            TestContext.Current.CancellationToken
        );
        var inactiveResult = await handler.HandleAsync(
            new ForgotPasswordCommand { Email = "inactive@respira.ams" },
            TestContext.Current.CancellationToken
        );

        Assert.False(missingResult.IsSuccess());
        Assert.Equal(ApplicationStatus.BadRequest, missingResult.StatusCode);
        Assert.False(inactiveResult.IsSuccess());
        Assert.Equal(ApplicationStatus.BadRequest, inactiveResult.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ReplacesPreviousResetTokens_SendsEmail_SetsCooldown_AndPublishesAudit()
    {
        var account = await SeedAccountAsync("doctor@respira.ams");
        var oldResetToken = new Token
        {
            HashToken = _hashService.HashToken("old-reset-token"),
            AccountId = account.Id,
            TokenType = TokenType.PasswordResetToken,
            ExpirationDate = DateTimeOffset.UtcNow.AddHours(1),
        };
        _context.Tokens.Add(oldResetToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        string? capturedBody = null;
        _emailServiceMock
            .Setup(x =>
                x.SendAsync(
                    account.Email,
                    "Reset your password",
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<string, string, string, CancellationToken>(
                (_, _, body, _) => capturedBody = body
            )
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new ForgotPasswordCommand { Email = "  DOCTOR@respira.ams  " },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.True(result.Data);
        Assert.NotNull(capturedBody);
        Assert.Contains("https://respira.ams/reset-password?token=", capturedBody);

        await using var verifyContext = new AuthDbContext(_options);
        var tokens = await verifyContext
            .Tokens.Where(x =>
                x.AccountId == account.Id && x.TokenType == TokenType.PasswordResetToken
            )
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Single(tokens);
        Assert.NotEqual(oldResetToken.HashToken, tokens[0].HashToken);

        _cacheServiceMock.Verify(
            x =>
                x.SetAsync(
                    "auth:account:reset:cooldown:doctor@respira.ams",
                    "1",
                    TimeSpan.FromMinutes(1),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _busMock.Verify(
            x =>
                x.PublishAsync(
                    It.Is<AuditEvent>(e => e.Action == "Authentication.PasswordResetEmailSent"),
                    It.IsAny<DeliveryOptions?>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleAsync_RollsBackIssuedToken_AndReturnsThirdPartyFailure_WhenEmailSendThrows()
    {
        var account = await SeedAccountAsync("smtp-fail@respira.ams");
        _emailServiceMock
            .Setup(x =>
                x.SendAsync(
                    account.Email,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new InvalidOperationException("SMTP connection refused"));

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new ForgotPasswordCommand { Email = account.Email },
            TestContext.Current.CancellationToken
        );

        Assert.False(result.IsSuccess());
        Assert.Equal(ApplicationStatus.ThirdPartyServiceFailure, result.StatusCode);

        await using var verifyContext = new AuthDbContext(_options);
        var tokens = await verifyContext
            .Tokens.Where(x =>
                x.AccountId == account.Id && x.TokenType == TokenType.PasswordResetToken
            )
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Empty(tokens);
    }

    private ForgotPasswordCommandHandler CreateHandler() =>
        new(
            _context,
            _hashService,
            _emailServiceMock.Object,
            _cacheServiceMock.Object,
            _resetOption,
            NullLogger<ForgotPasswordCommandHandler>.Instance,
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
            HashPassword = _hashService.HashPassword("Respira@123"),
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
