using Microsoft.EntityFrameworkCore;
using Respira.Doctor.Infrastructure.Persistence;

namespace Respira.Doctor.API.Extensions
{
    public static class WebApplicationExtensions
    {
        /// <summary>Applies pending EF Core migrations before the API begins accepting requests.</summary>
        public static async Task ApplyMigrationsAsync(
            this WebApplication app,
            CancellationToken cancellationToken = default
        )
        {
            await using var scope = app.Services.CreateAsyncScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DoctorDbContext>>();
            var dbContext = scope.ServiceProvider.GetRequiredService<DoctorDbContext>();

            try
            {
                await dbContext.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Doctor database migrations applied successfully");
            }
            catch (Exception exception)
            {
                logger.LogCritical(exception, "Unable to apply doctor database migrations");
                throw;
            }
        }
    }
}
