using Authentication.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Authentication.API.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>Applies pending EF Core migrations before the API begins accepting requests.</summary>
    public static async Task ApplyMigrationsAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default
    )
    {
        await using var scope = app.Services.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AuthDbContext>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        try
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Authentication database migrations applied successfully");
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "Unable to apply authentication database migrations");
            throw;
        }
    }
}
