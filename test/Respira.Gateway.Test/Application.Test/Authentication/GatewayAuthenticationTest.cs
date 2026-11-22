using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Respira.Gateway.Application.Test.Authentication;

/// <summary>
/// End-to-end tests of the gateway pipeline: the fallback policy authenticates every
/// proxied route, the configured public routes stay anonymous, and the identity
/// headers forwarded to the services always come from the validated token.
///
/// A stub destination listens on loopback and echoes the identity headers it
/// receives, so every assertion below observes what a downstream service would see.
/// </summary>
public sealed class GatewayAuthenticationTest : IAsyncLifetime
{
    private const string JwtSecret = "respira-gateway-integration-test-secret-key-0123456789";
    private const string JwtIssuer = "Respira AMS";
    private const string JwtAudience = "respira-clients";
    private const string AllowedOrigin = "http://localhost:3000";
    private const string AccountId = "0193f0aa-8f0f-7c3a-9b0e-3d1f0a2b4c6d";
    private const string Email = "admin@respira.ams";

    private static readonly string[] EnvironmentKeys =
    [
        "Jwt__Secret",
        "Jwt__Issuer",
        "Jwt__Audience",
        "CORS__AllowedOrigins__0",
        "CORS__AllowedMethods__0",
        "CORS__AllowedMethods__1",
        "CORS__AllowedMethods__2",
        "CORS__AllowedHeaders__0",
        "CORS__AllowedHeaders__1",
        "CORS__AllowedHeaders__2",
        "ReverseProxy__Clusters__clinical-cluster__Destinations__clinical__Address",
        "ReverseProxy__Clusters__auth-cluster__Destinations__auth__Address",
    ];

    private WebApplication _destination = null!;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        _destination = await StartEchoDestinationAsync();

        SetEnvironment("Jwt__Secret", JwtSecret);
        SetEnvironment("Jwt__Issuer", JwtIssuer);
        SetEnvironment("Jwt__Audience", JwtAudience);
        SetEnvironment("CORS__AllowedOrigins__0", AllowedOrigin);
        SetEnvironment("CORS__AllowedMethods__0", "GET");
        SetEnvironment("CORS__AllowedMethods__1", "POST");
        SetEnvironment("CORS__AllowedMethods__2", "OPTIONS");
        SetEnvironment("CORS__AllowedHeaders__0", "Content-Type");
        SetEnvironment("CORS__AllowedHeaders__1", "Authorization");
        SetEnvironment("CORS__AllowedHeaders__2", "X-Role");
        SetEnvironment(
            "ReverseProxy__Clusters__clinical-cluster__Destinations__clinical__Address",
            _destination.Urls.Single()
        );
        SetEnvironment(
            "ReverseProxy__Clusters__auth-cluster__Destinations__auth__Address",
            _destination.Urls.Single()
        );

        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _destination.DisposeAsync();

        foreach (var key in EnvironmentKeys)
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }

    #region Authenticated routes

    [Fact]
    public async Task ProxiedRoute_WithoutToken_IsRejectedAtTheGateway()
    {
        var response = await _client.GetAsync("/api/v1/antibiotics", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProxiedRoute_WithAForgedSigningKey_IsRejectedAtTheGateway()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/antibiotics");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateToken("Admin", "a-completely-different-attacker-secret-0123456789abcdef")
        );

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProxiedRoute_WithATokenAndForgedHeaders_ForwardsTheTokenIdentity()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/antibiotics");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateToken("Doctor")
        );
        request.Headers.TryAddWithoutValidation("X-ID", "11111111-1111-1111-1111-111111111111");
        request.Headers.TryAddWithoutValidation("X-Email", "attacker@example.com");
        request.Headers.TryAddWithoutValidation("X-Role", "MedicalRegulationReviewer");

        var echoed = await SendAndReadEchoAsync(request);

        Assert.Equal(AccountId, echoed.Id);
        Assert.Equal(Email, echoed.Email);
        Assert.Equal("Doctor", echoed.Role);
    }

    [Fact]
    public async Task ProxiedRoute_WithAnAdminToken_ForwardsTheAdminRole()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/clinical/audit-logs");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateToken("Admin")
        );

        var echoed = await SendAndReadEchoAsync(request);

        Assert.Equal("Admin", echoed.Role);
    }

    #endregion

    #region Public and infrastructure endpoints

    [Theory]
    [InlineData("POST", "/api/v1/auth/login")]
    [InlineData("POST", "/api/v1/auth/logout")]
    [InlineData("POST", "/api/v1/auth/refresh")]
    [InlineData("POST", "/api/v1/auth/email-verification")]
    [InlineData("POST", "/api/v1/auth/email-verification/confirm")]
    [InlineData("POST", "/api/v1/auth/forgot-password")]
    [InlineData("POST", "/api/v1/auth/forgot-password/confirm")]
    public async Task PublicAuthRoutes_AreReachableWithoutAToken(string method, string path)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        request.Content = JsonContent.Create(new { });

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AccountManagementRoute_RequiresAToken()
    {
        var response = await _client.GetAsync("/api/v1/auth/accounts", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AccountManagementRoute_WithAToken_IsProxied()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/accounts");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateToken("Admin")
        );

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthEndpoint_IsReachableWithoutAToken()
    {
        var response = await _client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OpenApiDocument_IsReachableWithoutAToken()
    {
        var response = await _client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region CORS

    [Fact]
    public async Task CorsPreflight_IsAnsweredBeforeAuthentication()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/antibiotics");
        request.Headers.TryAddWithoutValidation("Origin", AllowedOrigin);
        request.Headers.TryAddWithoutValidation("Access-Control-Request-Method", "GET");
        request.Headers.TryAddWithoutValidation("Access-Control-Request-Headers", "authorization");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.True(
            response.IsSuccessStatusCode,
            $"Expected a successful preflight, got {(int)response.StatusCode}."
        );
        Assert.Equal(AllowedOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    #endregion

    private async Task<EchoedIdentity> SendAndReadEchoAsync(HttpRequestMessage request)
    {
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var echoed = await response.Content.ReadFromJsonAsync<EchoedIdentity>(TestContext.Current.CancellationToken);
        Assert.NotNull(echoed);
        return echoed;
    }

    private static string CreateToken(string role, string? secret = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret ?? JwtSecret));
        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, AccountId),
                new Claim(JwtRegisteredClaimNames.Email, Email),
                new Claim(ClaimTypes.NameIdentifier, AccountId),
                new Claim(ClaimTypes.Email, Email),
                new Claim(ClaimTypes.Role, role),
            ],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static async Task<WebApplication> StartEchoDestinationAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();

        var app = builder.Build();
        app.Run(async context =>
        {
            await context.Response.WriteAsJsonAsync(
                new EchoedIdentity(
                    context.Request.Headers["X-ID"].ToString(),
                    context.Request.Headers["X-Email"].ToString(),
                    context.Request.Headers["X-Role"].ToString()
                )
            );
        });

        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }

    private static void SetEnvironment(string key, string value) =>
        Environment.SetEnvironmentVariable(key, value);

    public sealed record EchoedIdentity(string Id, string Email, string Role);
}
