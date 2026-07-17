using Microsoft.Extensions.DependencyInjection;
using Respira.ServiceDefaults.Middlewares;

namespace Respira.ServiceDefaults.Extensions;

/// <summary>
/// Configure OpenApi extensions with custom document transformer
/// </summary>
public static class OpenApiExtension
{
    public static IServiceCollection AddOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
        return services;
    }
}