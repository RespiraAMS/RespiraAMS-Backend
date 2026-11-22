using System.Net.Http.Headers;
using System.Security.Claims;
using Respira.Gateway.Identity;

namespace Respira.Gateway.Application.Test.Identity;

/// <summary>
/// Covers the gateway identity projection: the transform must always discard the
/// headers supplied by the client and rebuild them from the validated principal.
/// </summary>
public sealed class GatewayIdentityHeadersTest
{
    private const string AccountId = "0193f0aa-8f0f-7c3a-9b0e-3d1f0a2b4c6d";
    private const string Email = "admin@respira.ams";

    #region Unauthenticated callers

    [Fact]
    public void Apply_WithoutAuthentication_RemovesClientSuppliedHeaders()
    {
        var headers = CreateHeaders(
            (GatewayIdentityHeaders.IdHeader, AccountId),
            (GatewayIdentityHeaders.EmailHeader, Email),
            (GatewayIdentityHeaders.RoleHeader, "Admin")
        );

        GatewayIdentityHeaders.Apply(new ClaimsPrincipal(new ClaimsIdentity()), headers);

        Assert.False(headers.Contains(GatewayIdentityHeaders.IdHeader));
        Assert.False(headers.Contains(GatewayIdentityHeaders.EmailHeader));
        Assert.False(headers.Contains(GatewayIdentityHeaders.RoleHeader));
    }

    [Fact]
    public void Apply_WithoutAuthentication_KeepsUnrelatedHeaders()
    {
        var headers = CreateHeaders(("X-Request-Id", "trace-1"));

        GatewayIdentityHeaders.Apply(new ClaimsPrincipal(new ClaimsIdentity()), headers);

        Assert.Equal("trace-1", headers.GetValues("X-Request-Id").Single());
    }

    #endregion

    #region Authenticated callers

    [Fact]
    public void Apply_WithAuthentication_WritesTheClaimsDerivedHeaders()
    {
        var headers = CreateHeaders();

        GatewayIdentityHeaders.Apply(CreatePrincipal("Doctor"), headers);

        Assert.Equal(AccountId, headers.GetValues(GatewayIdentityHeaders.IdHeader).Single());
        Assert.Equal(Email, headers.GetValues(GatewayIdentityHeaders.EmailHeader).Single());
        Assert.Equal("Doctor", headers.GetValues(GatewayIdentityHeaders.RoleHeader).Single());
    }

    [Fact]
    public void Apply_WithClientSuppliedRole_UsesTheTokenRole()
    {
        var headers = CreateHeaders(
            (GatewayIdentityHeaders.IdHeader, "11111111-1111-1111-1111-111111111111"),
            (GatewayIdentityHeaders.EmailHeader, "attacker@example.com"),
            (GatewayIdentityHeaders.RoleHeader, "MedicalRegulationReviewer")
        );

        GatewayIdentityHeaders.Apply(CreatePrincipal("Admin"), headers);

        Assert.Equal(AccountId, headers.GetValues(GatewayIdentityHeaders.IdHeader).Single());
        Assert.Equal(Email, headers.GetValues(GatewayIdentityHeaders.EmailHeader).Single());
        Assert.Equal("Admin", headers.GetValues(GatewayIdentityHeaders.RoleHeader).Single());
    }

    [Fact]
    public void Apply_WithMultipleRoles_JoinsThemForTheDownstreamParser()
    {
        var headers = CreateHeaders();

        GatewayIdentityHeaders.Apply(CreatePrincipal("Doctor", "HeadDepartmentDoctor"), headers);

        Assert.Equal(
            "Doctor,HeadDepartmentDoctor",
            headers.GetValues(GatewayIdentityHeaders.RoleHeader).Single()
        );
    }

    [Fact]
    public void Apply_WithShortJwtClaimNames_UsesThemAsFallback()
    {
        var headers = CreateHeaders();
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim("sub", AccountId),
                    new Claim("email", Email),
                    new Claim("role", "Doctor"),
                ],
                authenticationType: "Bearer"
            )
        );

        GatewayIdentityHeaders.Apply(principal, headers);

        Assert.Equal(AccountId, headers.GetValues(GatewayIdentityHeaders.IdHeader).Single());
        Assert.Equal(Email, headers.GetValues(GatewayIdentityHeaders.EmailHeader).Single());
        Assert.Equal("Doctor", headers.GetValues(GatewayIdentityHeaders.RoleHeader).Single());
    }

    [Fact]
    public void Apply_WithWhitespaceClaims_DoesNotForwardThem()
    {
        var headers = CreateHeaders();
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim("sub", "  "), new Claim(ClaimTypes.Role, " ")], "Bearer")
        );

        GatewayIdentityHeaders.Apply(principal, headers);

        Assert.False(headers.Contains(GatewayIdentityHeaders.IdHeader));
        Assert.False(headers.Contains(GatewayIdentityHeaders.EmailHeader));
        Assert.False(headers.Contains(GatewayIdentityHeaders.RoleHeader));
    }

    #endregion

    private static ClaimsPrincipal CreatePrincipal(params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, AccountId),
            new(ClaimTypes.Email, Email),
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
    }

    private static HttpRequestHeaders CreateHeaders(params (string Name, string Value)[] values)
    {
        var message = new HttpRequestMessage();
        foreach (var (name, value) in values)
        {
            message.Headers.TryAddWithoutValidation(name, value);
        }

        return message.Headers;
    }
}
