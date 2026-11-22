using System.Net.Http.Headers;
using System.Security.Claims;

namespace Respira.Gateway.Identity
{
    /// <summary>
    /// Projects the authenticated gateway principal onto the identity headers the
    /// downstream services authorize from.
    /// The gateway is the only component allowed to name the caller, so any inbound
    /// copy of these headers is discarded before the validated claims are written.
    /// <see cref="HttpRequestHeaders.TryAddWithoutValidation(string, string)"/> alone
    /// would keep a client-supplied value because the header already exists.
    /// </summary>
    public static class GatewayIdentityHeaders
    {
        public const string IdHeader = "X-ID";
        public const string EmailHeader = "X-Email";
        public const string RoleHeader = "X-Role";

        /// <summary>
        /// Removes the inbound identity headers and, when the caller is authenticated,
        /// rebuilds them from the validated claims.
        /// </summary>
        /// <param name="user">Principal produced by the gateway authentication handler.</param>
        /// <param name="headers">Outgoing (proxied) request headers.</param>
        public static void Apply(ClaimsPrincipal user, HttpRequestHeaders headers)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(headers);

            Remove(headers);

            if (user.Identity?.IsAuthenticated != true)
            {
                return;
            }

            var userId =
                user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
            var email = user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email");
            var roles = user.FindAll(ClaimTypes.Role)
                .Select(static claim => claim.Value)
                .Where(static role => !string.IsNullOrWhiteSpace(role))
                .ToArray();

            if (roles.Length == 0)
            {
                // Some handlers keep the short JWT claim names instead of mapping
                // them onto ClaimTypes.Role.
                roles = [.. user.FindAll("role")
                    .Concat(user.FindAll("roles"))
                    .Select(static claim => claim.Value)
                    .Where(static role => !string.IsNullOrWhiteSpace(role))];
            }

            if (!string.IsNullOrWhiteSpace(userId))
            {
                headers.TryAddWithoutValidation(IdHeader, userId);
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                headers.TryAddWithoutValidation(EmailHeader, email);
            }

            if (roles.Length > 0)
            {
                // ClaimsPropagationMiddleware splits the value on commas, so a
                // multi-role token round-trips as multiple role claims downstream.
                headers.TryAddWithoutValidation(RoleHeader, string.Join(',', roles));
            }
        }

        /// <summary>Drops any identity header that reached the gateway.</summary>
        /// <param name="headers">Outgoing (proxied) request headers.</param>
        public static void Remove(HttpRequestHeaders headers)
        {
            ArgumentNullException.ThrowIfNull(headers);

            headers.Remove(IdHeader);
            headers.Remove(EmailHeader);
            headers.Remove(RoleHeader);
        }
    }
}
