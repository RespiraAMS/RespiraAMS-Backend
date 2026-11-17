using Asp.Versioning;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Respira.ServiceDefaults.Utils.OpenApiTransformers;

namespace Respira.ServiceDefaults.Extensions
{
    public static class ApiVersioningExtension
    {
        public static void AddApiVersion(this IServiceCollection services)
        {
            services
                .AddApiVersioning(options =>
                {
                    options.ReportApiVersions = true;
                    options.ApiVersionReader = new UrlSegmentApiVersionReader();
                })
                .AddMvc()
                .AddApiExplorer(options =>
                {
                    options.GroupNameFormat = "'v'VVV";
                    options.SubstituteApiVersionInUrl = true;
                })
                .AddOpenApi();
            services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer<CustomDocumentTransformer>();
                options.AddSchemaTransformer<CustomSchemaTransformer>();
            });
        }

        public static void UseApiDocumentation(this WebApplication app)
        {
            app.MapOpenApi().WithDocumentPerVersion();
            app.MapScalarApiReference(opts => opts.Theme = ScalarTheme.Kepler);
        }
    }
}
