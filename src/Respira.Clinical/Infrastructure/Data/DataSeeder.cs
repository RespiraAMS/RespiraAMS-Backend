using System.Text.Json;
using Respira.Domain.Entities;
using Respira.Domain.Enums;

namespace Respira.Infrastructure.Data
{
    public class SeedData
    {
        public required List<ClinicalVariable> ClinicalVariables { get; init; }
    }

    public static class DataSeeder
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        public static async Task<SeedData> LoadAsync(string filePath)
        {
            var path = Path.IsPathRooted(filePath)
                ? filePath
                : Path.Combine(AppContext.BaseDirectory, filePath);

            var json = await File.ReadAllTextAsync(path);

            var dto = JsonSerializer.Deserialize<SeedDataDto>(json, JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize seed data.");

            return MapToDomain(dto);
        }

        private static SeedData MapToDomain(SeedDataDto dto)
        {
            var variables = dto.ClinicalVariables.ConvertAll(v =>
            {
                var variableId = GenerateId(v.Id);
                return new ClinicalVariable
                {
                    Id = variableId,
                    Name = v.Name,
                    Code = v.Code,
                    Description = v.Description,
                    ValueType = ParseEnum(v.ValueType, ClinicalValueType.Boolean),
                    CanonicalUnit = v.CanonicalUnit,
                };

            });

            return new SeedData
            {
                ClinicalVariables = variables,
            };
        }

        private static Guid GenerateId(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? Guid.CreateVersion7() : Guid.Parse(id);
        }

        private static T ParseEnum<T>(string value, T fallback) where T : struct, Enum
        {
            return Enum.TryParse(value, true, out T result) ? result : fallback;
        }
    }
}
