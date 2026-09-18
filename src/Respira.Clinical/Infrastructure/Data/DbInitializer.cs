using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Respira.Clinical.Infrastructure.Data
{
    public class DbInitializer
    {
        private static async Task<bool> HasAnyData(ClinicalDbContext context)
        {
            return await context.ClinicalVariables.AnyAsync() ||
                await context.Criteria.AnyAsync() ||
                await context.ScoreMetrics.AnyAsync() ||
                await context.Pathogens.AnyAsync() ||
                await context.RiskFactors.AnyAsync() ||
                await context.SuspectedCauses.AnyAsync() ||
                await context.AntibioticGroups.AnyAsync() ||
                await context.Antibiotics.AnyAsync() ||
                await context.Treatments.AnyAsync();
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
            await context.Pathogens.AddRangeAsync(seedData.Pathogens);
            await context.RiskFactors.AddRangeAsync(seedData.RiskFactors);
            await context.SuspectedCauses.AddRangeAsync(seedData.SuspectedCauses);
            await context.AntibioticGroups.AddRangeAsync(seedData.AntibioticGroups);
            await context.Antibiotics.AddRangeAsync(seedData.Antibiotics);
            await context.Treatments.AddRangeAsync(seedData.Treatments);

            var count = await context.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} records into database", count);
        }
    }
}
