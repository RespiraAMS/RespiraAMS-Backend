using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Respira.Application.Contracts.Data;
using Respira.Domain.Services;
using Respira.Infrastructure.Data;

namespace Respira.DI
{
    public static class DependencyInjection
    {
        public static void AddDI(this IHostApplicationBuilder builder)
        {
            AddDomain(builder);
            AddInfrastructure(builder);
        }

        # region Domain DI

        public static void AddDomain(this IHostApplicationBuilder builder)
        {
            builder.Services.AddScoped<IDiagnoseService, DiagnoseService>();
        }

        #endregion


        #region Infrastructure DI

        public static void AddInfrastructure(this IHostApplicationBuilder builder)
        {
            builder.AddNpgsqlDbContext<ClinicalDbContext>("clinicalDb");
            builder.Services.AddScoped<IDbContext, ClinicalDbContext>();
            builder.Services.Configure<SeedDataOptions>(builder.Configuration.GetSection(SeedDataOptions.SectionName));
        }

        public static void ApplyMigrations(this IHost host, bool isDevEnv)
        {
            using var scope = host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ClinicalDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DbInitializer>>();
            try
            {
                context.Database.Migrate();
            }
            catch (Exception e)
            {
                if (isDevEnv)
                {
                    context.Database.EnsureDeleted();
                }

                logger.LogCritical("Failed to migrate database: {error}", e.Message);
            }
        }

        public static async Task SeedData(this WebApplication app)
        {
            // Only seed data in dev environment
            if (app.Environment.IsDevelopment())
            {
                using var scope = app.Services.CreateScope();
                var provider = scope.ServiceProvider;
                var context = provider.GetRequiredService<ClinicalDbContext>();
                var options = provider.GetRequiredService<IOptions<SeedDataOptions>>();
                var logger = provider.GetRequiredService<ILogger<DbInitializer>>();
                await DbInitializer.InitializeAsync(context, options, logger);
            }
        }

        # endregion
    }
}
