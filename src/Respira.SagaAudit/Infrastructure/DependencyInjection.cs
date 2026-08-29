using Application.Abstracts.Data;
using Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Respira.SagaAudit.Application.Services;

namespace Respira.SagaAudit.Infrastructure
{
    /// <summary>
    /// DI registration for SagaAudit infrastructure layer.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers the SagaAudit database context and services.
        /// </summary>
        public static void AddInfrastructure(this IHostApplicationBuilder builder)
        {
            builder.AddNpgsqlDbContext<SagaAuditDbContext>("sagaAuditDb");
            builder.Services.AddScoped<ISagaAuditDbContext, SagaAuditDbContext>();
            builder.Services.AddScoped<ProcessTrackerService>();
        }

        /// <summary>
        /// Applies pending EF migrations. In dev, drops the database if migration fails.
        /// </summary>
        public static void ApplyMigrations(this IHost host, bool isDevEnv)
        {
            using var scope = host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SagaAuditDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<SagaAuditDbContext>>();
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
