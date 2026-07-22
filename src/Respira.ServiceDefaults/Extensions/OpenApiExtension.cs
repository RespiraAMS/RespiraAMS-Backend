using Microsoft.Extensions.DependencyInjection;
using Respira.ServiceDefaults.Utils.OpenApiTransformers;

namespace Respira.ServiceDefaults.Extensions;

/// <summary>
/// Configure OpenApi extensions with custom document transformer
/// </summary>
public static class OpenApiExtension
{
    public static IServiceCollection AddOpenApiExtension(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<CustomDocumentTransformer>();
            options.AddSchemaTransformer<CustomSchemaTransformer>();
        });
        return services;
    }
}