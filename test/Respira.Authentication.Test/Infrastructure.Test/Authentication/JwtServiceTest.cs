using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Respira.Authentication.Application.Contracts.Authentication;
using Respira.Authentication.Application.Contracts.Data;
using Respira.Authentication.Domain.Entities;
using Respira.Authentication.Domain.Enums;
using Respira.Authentication.Infrastructure.Authentication;
using Respira.Authentication.Infrastructure.Persistence;

namespace Respira.Authentication.Infrastructure.Test.Authentication;

public class JwtServiceAccessTokenTest
{
    private const string SecretKey =
        "super-secret-jwt-signing-key-with-at-least-64-bytes-for-hmac-sha512-security!!";
    private const string Issuer = "respira-auth-test";
    private const string Audience = "respira-clients-test";

    private static JwtOption CreateJwtOption(
        long accessTokenMinutes = 15,
        long refreshTokenDays = 7
    ) =>
        new()
        {
            Issuer = Issuer,
            Audience = Audience,
            SecretKey = SecretKey,
            AccessTokenExpires = accessTokenMinutes,
            RefreshTokenExpires = refreshTokenDays,
        };

    private static JwtService CreateService(JwtOption? option = null) =>
        new(
            Options.Create(option ?? CreateJwtOption()),
            Mock.Of<IAuthDbContext>(),
            new HashService()
        );

    private static Account CreateAccount(RoleType role = RoleType.Doctor) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            Email = "doctor@respira.ams",
            HashPassword = "$2a$12$hashedpassword",
            Phone = "0901234567",
            Role = role,
            IsEmailConfirmed = true,
            Status = StatusType.Active,
        };

    [Fact]
    public void GenerateAccessToken_ValidAccount_ProducesReadableJwtWithExpectedClaims()
    {
        var service = CreateService();
        var account = CreateAccount(RoleType.HeadDepartmentDoctor);

        var rawToken = service.GenerateAccessToken(account);

        Assert.False(string.IsNullOrWhiteSpace(rawToken));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(rawToken);
        Assert.Equal(Issuer, jwt.Issuer);
        Assert.Contains(Audience, jwt.Audiences);
        Assert.Equal(SecurityAlgorithms.HmacSha256, jwt.SignatureAlgorithm);
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == account.Id.ToString());
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Email && c.Value == account.Email);
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Jti && Guid.TryParse(c.Value, out _));
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == account.Id.ToString());
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Email && c.Value == account.Email);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == nameof(RoleType.HeadDepartmentDoctor));
    }

    [Theory]
    [InlineData(RoleType.Doctor)]
    [InlineData(RoleType.HeadDepartmentDoctor)]
    [InlineData(RoleType.MedicalRegulationReviewer)]
    [InlineData(RoleType.Admin)]
    public async Task ValidateAccessToken_GeneratedToken_ReturnsAccountIdAndRole(RoleType role)
    {
        var service = CreateService();
        var account = CreateAccount(role);
        var token = service.GenerateAccessToken(account);

        var (accountId, parsedRole) = await service.ValidateAccessToken(token);

        Assert.Equal(account.Id.ToString(), accountId);
        Assert.Equal(role, parsedRole);
    }

    [Fact]
    public async Task ValidateAccessToken_ExpiredToken_ThrowsSecurityTokenExpiredException()
    {
        var service = CreateService(CreateJwtOption(accessTokenMinutes: -5));
        var account = CreateAccount();
        var expiredToken = service.GenerateAccessToken(account);

        await Assert.ThrowsAnyAsync<SecurityTokenException>(() =>
            service.ValidateAccessToken(expiredToken)
        );
    }

    [Fact]
    public async Task ValidateAccessToken_WrongSigningKey_ThrowsSecurityTokenException()
    {
        var otherKeyOption = new JwtOption
        {
            Issuer = Issuer,
            Audience = Audience,
            SecretKey = "different-secret-signing-key-with-at-least-64-bytes-for-hmac-sha512-security!!",
            AccessTokenExpires = 15,
            RefreshTokenExpires = 7,
        };
        var token = CreateService(otherKeyOption).GenerateAccessToken(CreateAccount());
        var validator = CreateService();

        await Assert.ThrowsAnyAsync<SecurityTokenException>(() =>
            validator.ValidateAccessToken(token)
        );
    }

    [Fact]
    public async Task ValidateAccessToken_WrongIssuer_ThrowsSecurityTokenInvalidIssuerException()
    {
        var wrongIssuerOption = new JwtOption
        {
            Issuer = "wrong-issuer",
            Audience = Audience,
            SecretKey = SecretKey,
            AccessTokenExpires = 15,
            RefreshTokenExpires = 7,
        };
        var token = CreateService(wrongIssuerOption).GenerateAccessToken(CreateAccount());
        var validator = CreateService();

        await Assert.ThrowsAnyAsync<SecurityTokenException>(() =>
            validator.ValidateAccessToken(token)
        );
    }

    [Fact]
    public async Task ValidateAccessToken_WrongAudience_ThrowsSecurityTokenInvalidAudienceException()
    {
        var wrongAudienceOption = new JwtOption
        {
            Issuer = Issuer,
            Audience = "wrong-audience",
            SecretKey = SecretKey,
            AccessTokenExpires = 15,
            RefreshTokenExpires = 7,
        };
        var token = CreateService(wrongAudienceOption).GenerateAccessToken(CreateAccount());
        var validator = CreateService();

        await Assert.ThrowsAnyAsync<SecurityTokenException>(() =>
            validator.ValidateAccessToken(token)
        );
    }

    [Fact]
    public async Task ValidateAccessToken_MissingNameIdentifierClaim_ThrowsSecurityTokenException()
    {
        var service = CreateService();
        var token = MintCustomToken([new Claim(ClaimTypes.Role, nameof(RoleType.Doctor))]);

        var exception = await Assert.ThrowsAsync<SecurityTokenException>(() =>
            service.ValidateAccessToken(token)
        );

        Assert.Equal("Missing account ID claim.", exception.Message);
    }

    [Fact]
    public async Task ValidateAccessToken_MissingRoleClaim_ThrowsSecurityTokenException()
    {
        var service = CreateService();
        var token = MintCustomToken(
            [new Claim(ClaimTypes.NameIdentifier, Guid.CreateVersion7().ToString())]
        );

        var exception = await Assert.ThrowsAsync<SecurityTokenException>(() =>
            service.ValidateAccessToken(token)
        );

        Assert.Equal("Missing role claim.", exception.Message);
    }

    private static string MintCustomToken(IEnumerable<Claim> claims)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey)),
            SecurityAlgorithms.HmacSha256
        );
        var jwt = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials
        );
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}

public sealed class JwtServiceRefreshTokenTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private const string SecretKey =
        "super-secret-jwt-signing-key-with-at-least-64-bytes-for-hmac-sha512-security!!";
    private const string Issuer = "respira-auth-test";
    private const string Audience = "respira-clients-test";

    private readonly DbContextOptions<AuthDbContext> _dbOptions;
    private readonly AuthDbContext _context;
    private readonly HashService _hashService = new();

    public JwtServiceRefreshTokenTest(PostgresFixture fixture)
    {
        _dbOptions = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
        _context = new AuthDbContext(_dbOptions);
    }

    public async ValueTask InitializeAsync()
    {
        await _context
            .Tokens.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context
            .Accounts.IgnoreQueryFilters()
            .Where(a => a.Email != "admin@respira.ams")
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private JwtService CreateService(AuthDbContext context, long refreshTokenDays = 7) =>
        new(
            Options.Create(
                new JwtOption
                {
                    Issuer = Issuer,
                    Audience = Audience,
                    SecretKey = SecretKey,
                    AccessTokenExpires = 15,
                    RefreshTokenExpires = refreshTokenDays,
                }
            ),
            context,
            _hashService
        );

    private async Task<Account> SeedAccountAsync(
        RoleType role = RoleType.Doctor,
        StatusType status = StatusType.Active,
        bool isDeleted = false
    )
    {
        var account = new Account
        {
            Email = $"user-{Guid.CreateVersion7():N}@respira.ams",
            HashPassword = _hashService.HashPassword("Pass@123"),
            Phone = "0901234567",
            Role = role,
            IsEmailConfirmed = true,
            Status = status,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return account;
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_PersistsHashedRefreshTokenInDatabase()
    {
        var account = await SeedAccountAsync(RoleType.Admin);
        var service = CreateService(_context);

        var rawToken = await service.GenerateRefreshTokenAsync(account);

        Assert.False(string.IsNullOrWhiteSpace(rawToken));

        var expectedHash = _hashService.HashToken(rawToken);
        await using var verifyContext = new AuthDbContext(_dbOptions);
        var stored = await verifyContext
            .Tokens.AsNoTracking()
            .SingleAsync(t => t.AccountId == account.Id, TestContext.Current.CancellationToken);

        Assert.Equal(expectedHash, stored.HashToken);
        Assert.Equal(TokenType.RefreshToken, stored.TokenType);
        Assert.NotNull(stored.ExpirationDate);
        Assert.False(stored.IsExpired());
    }

    [Fact]
    public async Task ValidateRefreshToken_ValidActiveAccountAndStoredToken_ReturnsAccountIdAndRole()
    {
        var account = await SeedAccountAsync(RoleType.HeadDepartmentDoctor);
        var service = CreateService(_context);
        var rawToken = await service.GenerateRefreshTokenAsync(account);

        await using var verifyContext = new AuthDbContext(_dbOptions);
        var validator = CreateService(verifyContext);
        var (accountId, role) = await validator.ValidateRefreshToken(rawToken);

        Assert.Equal(account.Id.ToString(), accountId);
        Assert.Equal(RoleType.HeadDepartmentDoctor, role);
    }

    [Fact]
    public async Task ValidateRefreshToken_TokenNotInDatabase_ThrowsSecurityTokenException()
    {
        var account = await SeedAccountAsync();
        var service = CreateService(_context);
        var rawToken = await service.GenerateRefreshTokenAsync(account);

        await _context
            .Tokens.Where(t => t.AccountId == account.Id)
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);

        await using var verifyContext = new AuthDbContext(_dbOptions);
        var validator = CreateService(verifyContext);

        var exception = await Assert.ThrowsAsync<SecurityTokenException>(() =>
            validator.ValidateRefreshToken(rawToken)
        );
        Assert.Equal("Refresh token is invalid or expired.", exception.Message);
    }

    [Fact]
    public async Task ValidateRefreshToken_StoredTokenMarkedExpired_ThrowsSecurityTokenException()
    {
        var account = await SeedAccountAsync();
        var service = CreateService(_context);
        var rawToken = await service.GenerateRefreshTokenAsync(account);

        await _context
            .Tokens.Where(t => t.AccountId == account.Id)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.ExpirationDate, DateTimeOffset.UtcNow.AddMinutes(-10)),
                TestContext.Current.CancellationToken
            );

        await using var verifyContext = new AuthDbContext(_dbOptions);
        var validator = CreateService(verifyContext);

        var exception = await Assert.ThrowsAsync<SecurityTokenException>(() =>
            validator.ValidateRefreshToken(rawToken)
        );
        Assert.Equal("Refresh token is invalid or expired.", exception.Message);
    }

    [Fact]
    public async Task ValidateRefreshToken_InactiveAccount_ThrowsSecurityTokenException()
    {
        var account = await SeedAccountAsync(status: StatusType.Inactive);
        var service = CreateService(_context);
        var rawToken = await service.GenerateRefreshTokenAsync(account);

        await using var verifyContext = new AuthDbContext(_dbOptions);
        var validator = CreateService(verifyContext);

        var exception = await Assert.ThrowsAsync<SecurityTokenException>(() =>
            validator.ValidateRefreshToken(rawToken)
        );
        Assert.Equal("Account is inactive or no longer exists.", exception.Message);
    }

    [Fact]
    public async Task ValidateRefreshToken_SoftDeletedAccount_ThrowsSecurityTokenException()
    {
        var account = await SeedAccountAsync();
        var service = CreateService(_context);
        var rawToken = await service.GenerateRefreshTokenAsync(account);

        account.IsDeleted = true;
        account.DeletedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var verifyContext = new AuthDbContext(_dbOptions);
        var validator = CreateService(verifyContext);

        var exception = await Assert.ThrowsAsync<SecurityTokenException>(() =>
            validator.ValidateRefreshToken(rawToken)
        );
        Assert.Equal("Account is inactive or no longer exists.", exception.Message);
    }
}
