using Application.Abstracts.Caching;
using Application.Abstracts.Data;
using Infrastructure.Caching;
using Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure
{
    /// <summary>
    /// DI registration and startup helpers for the Doctor infrastructure layer
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers the Doctor database context, caching and services
        /// </summary>
        /// <param name="builder">Host application builder</param>
        public static void AddInfrastructure(this IHostApplicationBuilder builder)
        {
            builder.AddNpgsqlDbContext<DoctorDbContext>("doctorDb");

            builder.Services.AddFusionCache();

            builder.Services.AddScoped<IDoctorDbContext, DoctorDbContext>();
            builder.Services.AddScoped<ICacheService, CacheService>();
        }

        /// <summary>
        /// Applies pending EF migrations to the Doctor database. In dev, drops the database
        /// if migration fails so it can be recreated cleanly
        /// </summary>
        /// <param name="host">Host to resolve the Doctor database context from</param>
        /// <param name="isDevEnv">True in development environment</param>
        public static void ApplyMigrations(this IHost host, bool isDevEnv)
        {
            using var scope = host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DoctorDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DoctorDbContext>>();
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
    }
}
