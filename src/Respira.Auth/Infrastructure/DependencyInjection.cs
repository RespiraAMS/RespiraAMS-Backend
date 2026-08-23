using Application.Abstracts;
using Application.Abstracts.Authentication;
using Application.Abstracts.Caching;
using Application.Abstracts.Data;
using Application.Abstracts.Email;
using Infrastructure.Authentication;
using Infrastructure.Caching;
using Infrastructure.Email;
using Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure
{
    /// <summary>
    /// DI registration and startup helpers for the Auth infrastructure layer
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers the Auth database context, caching, services and option bindings
        /// </summary>
        /// <param name="builder">Host application builder</param>
        public static void AddInfrastructure(this IHostApplicationBuilder builder)
        {
            builder.AddNpgsqlDbContext<AuthDbContext>("authDb");

            builder.Services.AddFusionCache();

            builder.Services.AddScoped<IAuthDbContext, AuthDbContext>();
            builder.Services.AddScoped<Application.Features.Tokens.TokenRevoker>();
            builder.Services.AddScoped<IJwtService, JwtService>();
            builder.Services.AddScoped<IHashService, HashService>();
            builder.Services.AddScoped<ISendEmailService, SendEmailService>();
            builder.Services.AddScoped<ICacheService, CacheService>();
            builder.Services.AddScoped<IVerifyEmailLinkBuilder, VerifyEmailLinkBuilder>();

            builder.Services.Configure<JwtOption>(builder.Configuration.GetSection("Jwt"));
            builder.Services.Configure<EmailOption>(builder.Configuration.GetSection("Email"));
            builder.Services.Configure<VerifyEmailOption>(
                builder.Configuration.GetSection("VerifyEmail")
            );
            builder.Services.Configure<TokenCleanupOption>(
                builder.Configuration.GetSection("TokenCleanup")
            );
        }

        /// <summary>
        /// Applies pending EF migrations to the Auth database. In dev, drops the database
        /// if migration fails so it can be recreated cleanly
        /// </summary>
        /// <param name="host">Host to resolve the Auth database context from</param>
        /// <param name="isDevEnv">True in development environment</param>
        public static void ApplyMigrations(this IHost host, bool isDevEnv)
        {
            using var scope = host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AuthDbContext>>();
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
