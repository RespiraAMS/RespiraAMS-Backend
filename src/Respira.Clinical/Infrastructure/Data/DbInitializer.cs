using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Respira.Infrastructure.Data
{
    public class DbInitializer
    {
        private static async Task<bool> HasAnyData(ClinicalDbContext context)
        {
            return await context.ClinicalVariables.AnyAsync() ||
                await context.Criteria.AnyAsync() ||
                await context.ScoreMetrics.AnyAsync();
        }

        public static async Task InitializeAsync(ClinicalDbContext context, IOptions<SeedDataOptions> options, ILogger<DbInitializer> logger)
        {
            await context.Database.MigrateAsync();

            if (await HasAnyData(context))
            {
                logger.LogInformation("Database has data, skip seeding");
                return;
            }

            var seedData = await DataSeeder.LoadAsync(options.Value.FilePath);

            await context.ClinicalVariables.AddRangeAsync(seedData.ClinicalVariables);
            await context.Criteria.AddRangeAsync(seedData.Criteria);
            await context.ScoreMetrics.AddRangeAsync(seedData.ScoreMetrics);

            var count = await context.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} records into database", count);
        }
    }
}
