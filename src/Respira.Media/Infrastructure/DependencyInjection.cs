using Application.Abstracts.Data;
using Application.Abstracts.Storage;
using Cloudflare.NET.Core;
using Cloudflare.NET.R2;
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

        var r2Options =
            builder.Configuration.GetSection(R2Options.SectionName).Get<R2Options>()
            ?? new R2Options();

        if (string.IsNullOrWhiteSpace(r2Options.Endpoint))
        {
            builder.Services.AddScoped<IStorageService, LocalStorageService>();
        }
        else
        {
            var (accountId, endpointTemplate) = ToR2Endpoint(r2Options.Endpoint);

            builder.Services.AddCloudflareR2Client(options =>
            {
                options.AccessKeyId = r2Options.AccessKey;
                options.SecretAccessKey = r2Options.SecretKey;
                options.EndpointUrl = endpointTemplate;
                options.Region = "auto";
            });

            // The R2 client requires an AccountId to substitute into EndpointUrl ({0} placeholder).
            builder.Services.Configure<CloudflareApiOptions>(o => o.AccountId = accountId);

            builder.Services.AddScoped<IStorageService, CloudflareR2StorageService>();
        }

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

    /// <summary>
    ///   Splits a full R2 endpoint into the Account ID and an endpoint template containing a
    ///   <c>{0}</c> placeholder for the Account ID, as required by <c>Cloudflare.NET.R2</c>.
    /// </summary>
    private static (string AccountId, string EndpointTemplate) ToR2Endpoint(string endpoint)
    {
        try
        {
            var host = new Uri(endpoint).Host;
            var dot = host.IndexOf('.');
            if (dot < 0)
                return ("r2", endpoint);

            var accountId = host.Substring(0, dot);
            var rest = host.Substring(dot);
            return (accountId, $"https://{{0}}{rest}");
        }
        catch
        {
            return ("r2", endpoint);
        }
    }
}
