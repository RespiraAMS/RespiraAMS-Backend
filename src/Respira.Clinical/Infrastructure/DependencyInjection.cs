using Application.Contracts.Data;
using Application.Contracts.Mappers;
using Infrastructure.Data;
using Infrastructure.Data.Seeds;
using Infrastructure.Mappers;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<AppDbContext>("clinicalDb");
        builder.Services.AddScoped<IDbContext, AppDbContext>();
        builder.Services.AddScoped<IPaginationFactory, PaginationFactory>();
        builder.Services.Configure<SeedDataOptions>(builder.Configuration.GetSection(SeedDataOptions.SectionName));
    }

    public static void ApplyMigrations(this IHost host, bool isDevEnv)
    {
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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
            var context = provider.GetRequiredService<AppDbContext>();
            var options = provider.GetRequiredService<IOptions<SeedDataOptions>>();
            var logger = provider.GetRequiredService<ILogger<DbInitializer>>();
            await DbInitializer.InitializeAsync(context, options, logger);
        }
    }
}