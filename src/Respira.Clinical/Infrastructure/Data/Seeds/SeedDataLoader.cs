using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models;

namespace Infrastructure.Data.Seeds;

public static class SeedDataLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static async Task<SeedData> LoadAsync()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Infrastructure.Data.Seeds.seed-data.json";

        await using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");

        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();

        var dto = JsonSerializer.Deserialize<SeedDataDto>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize seed data.");

        return MapToDomain(dto);
    }

    private static SeedData MapToDomain(SeedDataDto dto)
    {
        var antibioticGroups = dto.AntibioticGroups.Select(g => new AntibioticGroup
        {
            Id = g.Id,
            Name = g.Name,
            Description = g.Description,
            ParentId = g.ParentId,
        }).ToList();

        var pathogens = dto.Pathogens.Select(p => new Pathogen
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
        }).ToList();

        return new SeedData
        {
            AntibioticGroups = antibioticGroups,
            Pathogens = pathogens,
        };
    }
}

public class SeedData
{
    public required List<AntibioticGroup> AntibioticGroups { get; init; }
    public required List<Pathogen> Pathogens { get; init; }
}