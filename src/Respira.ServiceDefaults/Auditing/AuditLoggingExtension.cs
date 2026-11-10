using Microsoft.Extensions.DependencyInjection;

namespace Respira.ServiceDefaults.Auditing;

public static class AuditLoggingExtension
{
    /// <summary>
    /// Registers the actor information used by AuditableDbContext.
    /// </summary>
    /// <remarks>
    /// The accessor is a singleton because EF Core resolves pooled DbContext constructor
    /// dependencies from the root provider. It is stateless and reads the current
    /// <see cref="Microsoft.AspNetCore.Http.HttpContext"/> from <c>IHttpContextAccessor</c>
    /// on every call, so request isolation is preserved.
    /// </remarks>
    public static IServiceCollection AddAuditLogging(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<IAuditContextAccessor, HttpContextAuditContextAccessor>();
        return services;
    }
}
