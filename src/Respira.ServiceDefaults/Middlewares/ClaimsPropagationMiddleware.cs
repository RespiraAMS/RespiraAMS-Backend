using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Respira.ServiceDefaults.Middlewares
{
    /// <summary>
    /// Extract identity from bearer token and downstream credential as request header from API gateway to service
    /// </summary>
    public class ClaimsPropagationMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            // headers. Identity is optional: service tokens may carry roles without
            // a user id.
            var claims = new List<Claim>();
            var userId = context.Request.Headers["X-ID"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
                claims.Add(new Claim("sub", userId));
            }

            claims.AddRange(
                context
                    .Request.Headers["X-Role"]
                    .ToString()
                    .Split(
                        ',',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                    )
                    .Select(role => new Claim(ClaimTypes.Role, role))
            );

            var email = context.Request.Headers["X-Email"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(email))
                claims.Add(new Claim(ClaimTypes.Email, email));

            if (claims.Count > 0)
                context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));

            await next(context);
        }
    }
}
