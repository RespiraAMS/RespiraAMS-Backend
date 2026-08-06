using Infrastructure.Data.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Data;

public class DbInitializer
{
    private static async Task<bool> HasAnyData(AppDbContext context)
    {
        return await context.AntibioticGroups.AnyAsync() || await context.Pathogens.AnyAsync();
    }

    public static async Task InitializeAsync(AppDbContext context, IOptions<SeedDataOptions> options,
        ILogger<DbInitializer> logger)
    {
        await context.Database.MigrateAsync();

        if (await HasAnyData(context))
        {
            logger.LogInformation("Database has data, skip seeding");
            return;
        }

        var seedData = await SeedDataLoader.LoadAsync(options.Value.FilePath);

        context.AntibioticGroups.AddRange(seedData.AntibioticGroups);
        context.Pathogens.AddRange(seedData.Pathogens);

        foreach (var antibiotic in seedData.Antibiotics)
        {
            context.Antibiotics.Add(antibiotic);
            context.Dosages.AddRange(antibiotic.Dosages);
        }

        context.Criteria.AddRange(seedData.Criteria);
        context.Diseases.AddRange(seedData.Diseases);

        foreach (var disease in seedData.Diseases)
        {
            foreach (var protocol in disease.EmpiricTreatmentProtocols)
            {
                context.UpdateRelations(protocol.OtherCriteria, protocol.OtherCriteriaIds);
                context.UpdateRelations(protocol.Medicines, protocol.MedicineIds);
            }
        }

        foreach (var antibiogram in seedData.Antibiograms)
        {
            context.Antibiograms.Add(antibiogram);
            context.UpdateRelations(antibiogram.Mics, antibiogram.MicIds);
            context.UpdateRelations(antibiogram.FirstPriorityMedicines, antibiogram.FirstPriorityMedicineIds);
            context.UpdateRelations(antibiogram.SecondPriorityMedicines, antibiogram.SecondPriorityMedicineIds);
        }

        var count = await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} records into database", count);
    }
}