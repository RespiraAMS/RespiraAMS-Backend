using Application.Contracts.Data;
using Application.Contracts.Mappers;
using Infrastructure.Data;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<AppDbContext>("clinicalDb");
        builder.Services.AddScoped<IDbContext, AppDbContext>();
        builder.Services.AddScoped<IPaginationFactory, PaginationFactory>();
    }

    public static void ApplyMigrations(this IHost host, bool isDevEnv)
    {
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        try
        {
            context.Database.Migrate();
        }
        catch (Exception)
        {
            if (isDevEnv)
            {
                context.Database.EnsureDeleted();
            }

            context.Database.Migrate();
        }
    }
}