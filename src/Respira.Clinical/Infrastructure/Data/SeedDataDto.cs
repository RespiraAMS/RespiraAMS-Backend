namespace Respira.Infrastructure.Data
{
    public record SeedDataDto
    {
        public List<ClinicalVariableDto> ClinicalVariables { get; init; } = [];
    }

    public record ClinicalVariableDto
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string ValueType { get; init; } = string.Empty;
        public string? CanonicalUnit { get; init; }
    }
}
