namespace Infrastructure.Data.Seeds;

public class SeedDataOptions
{
    public const string SectionName = "SeedData";

    /// <summary>
    /// Path to the seed data JSON file. When the path is relative, it's resolved against
    /// the application base directory (where content files are copied to).
    /// Different environments can override this value (e.g. appsettings.Development.json)
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
}
