using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Respira.ServiceDefaults.Auditing;

namespace Respira.ServiceDefaults.Application.Test.Auditing;

/// <summary>
/// Tests <see cref="HttpContextAuditContextAccessor"/>, which resolves the
/// audited actor from the claims reconstructed by the gateway middleware and
/// falls back to the gateway headers when authentication has not run yet.
/// </summary>
public sealed class HttpContextAuditContextAccessorTest
{
    #region No HTTP context

    [Fact]
    public void GetCurrent_WithoutAnHttpContext_ReturnsTheSystemContext()
    {
        var accessor = CreateAccessor();

        var context = accessor.GetCurrent();

        Assert.Null(context.ActorAccountId);
        Assert.Equal("System", context.ActorRole);
        Assert.Null(context.CorrelationId);
        Assert.Null(context.RequestPath);
    }

    #endregion

    #region Actor resolution

    [Fact]
    public void GetCurrent_WithANameIdentifierClaim_UsesTheClaimAsActor()
    {
        var actorId = Guid.CreateVersion7();
        var httpContext = CreateHttpContext([new Claim(ClaimTypes.NameIdentifier, actorId.ToString())]);

        var context = CreateAccessor(httpContext).GetCurrent();

        Assert.Equal(actorId, context.ActorAccountId);
    }

    [Fact]
    public void GetCurrent_WithASubjectClaim_UsesTheClaimAsActor()
    {
        var actorId = Guid.CreateVersion7();
        var httpContext = CreateHttpContext([new Claim("sub", actorId.ToString())]);

        var context = CreateAccessor(httpContext).GetCurrent();

        Assert.Equal(actorId, context.ActorAccountId);
    }

    [Fact]
    public void GetCurrent_WithBothClaims_PrefersTheNameIdentifierClaim()
    {
        var nameIdentifier = Guid.CreateVersion7();
        var subject = Guid.CreateVersion7();
        var httpContext = CreateHttpContext(
            [
                new Claim("sub", subject.ToString()),
                new Claim(ClaimTypes.NameIdentifier, nameIdentifier.ToString()),
            ]
        );

        var context = CreateAccessor(httpContext).GetCurrent();

        Assert.Equal(nameIdentifier, context.ActorAccountId);
    }

    [Fact]
    public void GetCurrent_WithoutClaims_UsesTheIdHeaderAsActor()
    {
        var actorId = Guid.CreateVersion7();
        var httpContext = CreateHttpContext(
            headers: new Dictionary<string, string> { ["X-ID"] = actorId.ToString() }
        );

        var context = CreateAccessor(httpContext).GetCurrent();

        Assert.Equal(actorId, context.ActorAccountId);
    }

    [Fact]
    public void GetCurrent_WithANonGuidIdHeader_LeavesTheActorUnresolved()
    {
        var httpContext = CreateHttpContext(
            headers: new Dictionary<string, string> { ["X-ID"] = "not-a-guid" }
        );

        var context = CreateAccessor(httpContext).GetCurrent();

        Assert.Null(context.ActorAccountId);
    }

    #endregion

    #region Role resolution

    [Fact]
    public void GetCurrent_WithARoleClaim_UsesTheClaimAsRole()
    {
        var httpContext = CreateHttpContext([new Claim(ClaimTypes.Role, "Nurse")]);

        var context = CreateAccessor(httpContext).GetCurrent();

        Assert.Equal("Nurse", context.ActorRole);
    }

    [Fact]
    public void GetCurrent_WithMultipleRoleClaims_JoinsThemWithAComma()
    {
        var httpContext = CreateHttpContext(
            [new Claim(ClaimTypes.Role, "Doctor"), new Claim(ClaimTypes.Role, "Nurse")]
        );

        var context = CreateAccessor(httpContext).GetCurrent();

        Assert.Equal("Doctor,Nurse", context.ActorRole);
    }

    [Fact]
    public void GetCurrent_WithDuplicateRoleClaims_DeduplicatesThem()
    {
        var httpContext = CreateHttpContext(
            [new Claim(ClaimTypes.Role, "Admin"), new Claim(ClaimTypes.Role, "admin")]
        );

        var context = CreateAccessor(httpContext).GetCurrent();

        Assert.Equal("Admin", context.ActorRole);
    }

    [Fact]
    public void GetCurrent_WithoutRoleClaims_UsesTheRoleHeader()
    {
        var httpContext = CreateHttpContext(
            headers: new Dictionary<string, string> { ["X-Role"] = "Pharmacist" }
        );

        var context = CreateAccessor(httpContext).GetCurrent();

        Assert.Equal("Pharmacist", context.ActorRole);
    }

    [Fact]
    public void GetCurrent_WithMultipleRoleHeaderValues_UsesAllOfThem()
    {
        var httpContext = CreateHttpContext(
            headers: new Dictionary<string, string> { ["X-Role"] = "Doctor, Nurse" }
        );

        var context = CreateAccessor(httpContext).GetCurrent();

        Assert.Equal("Doctor,Nurse", context.ActorRole);
    }

    [Fact]
    public void GetCurrent_WithRoleClaims_PrefersThemOverTheRoleHeader()
    {
        var httpContext = CreateHttpContext(
            [new Claim(ClaimTypes.Role, "Nurse")],
            headers: new Dictionary<string, string> { ["X-Role"] = "Pharmacist" }
        );

        var context = CreateAccessor(httpContext).GetCurrent();

        Assert.Equal("Nurse", context.ActorRole);
    }

    [Fact]
    public void GetCurrent_WithoutAnyRole_ReturnsUnknown()
    {
        var httpContext = CreateHttpContext();

        var context = CreateAccessor(httpContext).GetCurrent();

        Assert.Equal("Unknown", context.ActorRole);
    }

    #endregion

    #region Correlation and path

    [Fact]
    public void GetCurrent_WithACorrelationIdHeader_UsesTheHeader()
    {
        var correlationId = Guid.CreateVersion7();
        var httpContext = CreateHttpContext(
            headers: new Dictionary<string, string> { ["X-Correlation-ID"] = correlationId.ToString() }
        );

        var context = CreateAccessor(httpContext).GetCurrent();

        Assert.Equal(correlationId, context.CorrelationId);
    }

    [Fact]
    public void GetCurrent_WithOnlyARequestIdHeader_UsesTheHeader()
    {
        var correlationId = Guid.CreateVersion7();
        var httpContext = CreateHttpContext(
            headers: new Dictionary<string, string> { ["X-Request-ID"] = correlationId.ToString() }
        );

        var context = CreateAccessor(httpContext).GetCurrent();

        Assert.Equal(correlationId, context.CorrelationId);
    }

    [Fact]
    public void GetCurrent_WithBothCorrelationHeaders_PrefersTheCorrelationIdHeader()
    {
        var correlationId = Guid.CreateVersion7();
        var requestId = Guid.CreateVersion7();
        var httpContext = CreateHttpContext(
            headers: new Dictionary<string, string>
            {
                ["X-Correlation-ID"] = correlationId.ToString(),
                ["X-Request-ID"] = requestId.ToString(),
            }
        );

        var context = CreateAccessor(httpContext).GetCurrent();

        Assert.Equal(correlationId, context.CorrelationId);
    }

    [Fact]
    public void GetCurrent_UsesTheRequestPath()
    {
        var httpContext = CreateHttpContext(path: "/api/v1/antibiotics");

        var context = CreateAccessor(httpContext).GetCurrent();

        Assert.Equal("/api/v1/antibiotics", context.RequestPath);
    }

    [Fact]
    public void GetCurrent_WithAVeryLongPath_TruncatesIt()
    {
        var path = "/" + new string('a', 600);
        var httpContext = CreateHttpContext(path: path);

        var context = CreateAccessor(httpContext).GetCurrent();

        Assert.NotNull(context.RequestPath);
        Assert.Equal(512, context.RequestPath.Length);
        Assert.Equal(path[..512], context.RequestPath);
    }

    [Fact]
    public void GetCurrent_WithAVeryLongRole_TruncatesIt()
    {
        var role = new string('r', 100);
        var httpContext = CreateHttpContext([new Claim(ClaimTypes.Role, role)]);

        var context = CreateAccessor(httpContext).GetCurrent();

        Assert.Equal(64, context.ActorRole.Length);
        Assert.Equal(role[..64], context.ActorRole);
    }

    #endregion

    #region Ambient scope

    [Fact]
    public void GetCurrent_WithAnAuditContextScope_OverridesTheHttpContext()
    {
        var actorId = Guid.CreateVersion7();
        var correlationId = Guid.CreateVersion7();
        var httpContext = CreateHttpContext(
            [new Claim(ClaimTypes.NameIdentifier, Guid.CreateVersion7().ToString())],
            headers: new Dictionary<string, string> { ["X-Role"] = "Pharmacist" }
        );
        var accessor = CreateAccessor(httpContext);

        using (AuditContextScope.Begin(
            new AuditContext(actorId, "Admin", correlationId, "/scoped/path", "trace-scoped")
        ))
        {
            var context = accessor.GetCurrent();

            Assert.Equal(actorId, context.ActorAccountId);
            Assert.Equal("Admin", context.ActorRole);
            Assert.Equal(correlationId, context.CorrelationId);
            Assert.Equal("/scoped/path", context.RequestPath);
            Assert.Equal("trace-scoped", context.TraceId);
        }
    }

    [Fact]
    public void GetCurrent_AfterTheAuditContextScopeIsDisposed_RestoresThePreviousScope()
    {
        var outerActor = Guid.CreateVersion7();
        var innerActor = Guid.CreateVersion7();
        var accessor = CreateAccessor(CreateHttpContext());

        using (AuditContextScope.Begin(new AuditContext(outerActor, "Outer", null, null, null)))
        {
            using (AuditContextScope.Begin(new AuditContext(innerActor, "Inner", null, null, null)))
            {
                Assert.Equal(innerActor, accessor.GetCurrent().ActorAccountId);
            }

            Assert.Equal(outerActor, accessor.GetCurrent().ActorAccountId);
        }

        Assert.Null(AuditContextScope.Current);
    }

    #endregion

    private static HttpContextAuditContextAccessor CreateAccessor(HttpContext? httpContext = null)
    {
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        return new HttpContextAuditContextAccessor(httpContextAccessor);
    }

    private static DefaultHttpContext CreateHttpContext(
        IEnumerable<Claim>? claims = null,
        IDictionary<string, string>? headers = null,
        string path = "/api/v1/antibiotics"
    )
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims ?? []));
        httpContext.Request.Path = path;

        if (headers is not null)
        {
            foreach (var header in headers)
                httpContext.Request.Headers[header.Key] = header.Value;
        }

        return httpContext;
    }
}
