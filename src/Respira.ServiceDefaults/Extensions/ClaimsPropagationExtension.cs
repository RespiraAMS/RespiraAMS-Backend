using Microsoft.AspNetCore.Builder;
using Respira.ServiceDefaults.Middlewares;

namespace Respira.ServiceDefaults.Extensions;

/// <summary>
/// An extensions that will register and activate the <see cref="ClaimsPropagationMiddleware"/> middleware
/// </summary>
public static class ClaimsPropagationExtensions
{
    public static IApplicationBuilder UseClaimsPropagation(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ClaimsPropagationMiddleware>();
    }
}