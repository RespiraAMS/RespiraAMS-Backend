using Application.Abstracts.Data;
using Application.Abstracts.Storage;
using Infrastructure.Persistence.Database;
using Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure;

/// <summary>
/// DI registration and startup helpers for the Media infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the Media database context, storage service and configuration.
    /// </summary>
    /// <param name="builder">Host application builder</param>
    public static void AddInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<MediaDbContext>("mediaDb");

        builder.Services.AddScoped<IMediaDbContext, MediaDbContext>();
        builder.Services.AddScoped<IStorageService, CloudflareR2StorageService>();

        builder.Services.Configure<R2Options>(
            builder.Configuration.GetSection(R2Options.SectionName)
        );
    }

    /// <summary>
    /// Applies pending EF migrations to the Media database. In dev, drops the database
    /// if migration fails so it can be recreated cleanly.
    /// </summary>
    /// <param name="host">Host to resolve the Media database context from</param>
    /// <param name="isDevEnv">True in development environment</param>
    public static void ApplyMigrations(this IHost host, bool isDevEnv)
    {
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MediaDbContext>>();
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
