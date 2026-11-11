using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Respira.ServiceDefaults.Auditing;

/// <summary>
/// Resolves the actor from the claims reconstructed by <c>ClaimsPropagationMiddleware</c>.
/// It also falls back to the gateway headers so auditing keeps working even when the
/// middleware runs before authentication.
/// </summary>
public sealed class HttpContextAuditContextAccessor(IHttpContextAccessor httpContextAccessor)
    : IAuditContextAccessor
{
    private const int MaxRoleLength = 64;
    private const int MaxPathLength = 512;
    private const int MaxTraceIdLength = 64;

    public AuditContext GetCurrent()
    {
        if (AuditContextScope.Current is { } scopedContext)
        {
            return scopedContext;
        }

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return AuditContext.System with { TraceId = CurrentTraceId() };
        }

        var user = httpContext.User;
        var actorId =
            ParseGuid(user.FindFirstValue(ClaimTypes.NameIdentifier))
            ?? ParseGuid(user.FindFirstValue("sub"))
            ?? ParseGuid(httpContext.Request.Headers["X-ID"].FirstOrDefault());

        var role =
            JoinRoles(user.FindAll(ClaimTypes.Role).Select(claim => claim.Value))
            ?? JoinRoles(
                httpContext.Request.Headers["X-Role"]
                    .ToString()
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            )
            ?? "Unknown";

        return new AuditContext(
            actorId,
            Truncate(role, MaxRoleLength) ?? "Unknown",
            ParseGuid(httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault())
                ?? ParseGuid(httpContext.Request.Headers["X-Request-ID"].FirstOrDefault()),
            Truncate(httpContext.Request.Path.Value, MaxPathLength),
            Truncate(CurrentTraceId() ?? httpContext.TraceIdentifier, MaxTraceIdLength)
        );
    }

    private static string? CurrentTraceId() =>
        Activity.Current?.TraceId.ToHexString();

    private static Guid? ParseGuid(string? value) =>
        Guid.TryParse(value, out var parsed) ? parsed : null;

    private static string? JoinRoles(IEnumerable<string> roles)
    {
        var values = roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return values.Length == 0 ? null : string.Join(',', values);
    }

    private static string? Truncate(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Length <= maxLength
                ? value
                : value[..maxLength];
}
