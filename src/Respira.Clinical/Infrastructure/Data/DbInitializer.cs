using System.Diagnostics.CodeAnalysis;
using Infrastructure.Data.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data;

public class DbInitializer
{
    private static async Task<bool> HasAnyData(AppDbContext context)
    {
        return await context.AntibioticGroups.AnyAsync() || await context.Pathogens.AnyAsync();
    }

    public static async Task InitializeAsync(AppDbContext context, ILogger<DbInitializer> logger)
    {
        await context.Database.MigrateAsync();

        if (await HasAnyData(context))
        {
            logger.LogInformation("Database has data, skip seeding");
            return;
        }

        var seedData = await SeedDataLoader.LoadAsync();

        context.AntibioticGroups.AddRange(seedData.AntibioticGroups);
        context.Pathogens.AddRange(seedData.Pathogens);

        var count = await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} records into database", count);
    }
}