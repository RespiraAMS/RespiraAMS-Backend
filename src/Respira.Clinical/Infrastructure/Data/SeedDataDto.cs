namespace Respira.Infrastructure.Data
{
    public record SeedDataDto
    {
        public List<ClinicalVariableDto> ClinicalVariables { get; init; } = [];
        public List<ScoreMetricsDto> ScoreMetrics { get; init; } = [];
        public List<CriterionDto> Criteria { get; init; } = [];
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

    public record ScoreMetricsDto
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public List<ScoringRuleDto> ScoringRules { get; init; } = [];
    }

    public record ScoringRuleDto
    {
        public string Id { get; init; } = string.Empty;
        public string CriterionId { get; init; } = string.Empty;
        public FormulaDto ScoreFunction { get; init; } = null!;
    }

    public record CriterionDto
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public FormulaDto Formula { get; init; } = null!;
    }

    public record FormulaDto
    {
        public string ResultType { get; init; } = string.Empty;
        public System.Text.Json.JsonElement? Constant { get; init; }
        public FormulaVariableDto? Variable { get; init; }
        public FormulaDto? Left { get; init; }
        public FormulaDto? Right { get; init; }
        public FormulaDto? Operand { get; init; }
        public string? Operator { get; init; }
        public FormulaDto? Condition { get; init; }
        public FormulaDto? IfTrue { get; init; }
        public FormulaDto? IfFalse { get; init; }
    }

    public record FormulaVariableDto
    {
        public string Id { get; init; } = string.Empty;
    }
}
