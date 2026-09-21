namespace Respira.Clinical.Infrastructure.Data
{
    public record SeedDataDto
    {
        public ICollection<ClinicalVariableDto> ClinicalVariables { get; init; } = [];
        public ICollection<ScoreMetricsDto> ScoreMetrics { get; init; } = [];
        public ICollection<CriterionDto> Criteria { get; init; } = [];
        public ICollection<PathogenDto> Pathogens { get; init; } = [];
        public ICollection<SuspectedCauseDto> SuspectedCauses { get; init; } = [];
        public ICollection<AntibioticGroupDto> AntibioticGroups { get; init; } = [];
        public ICollection<AntibioticDto> Antibiotics { get; init; } = [];
        public ICollection<TreatmentDto> Treatments { get; init; } = [];
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
        public ICollection<ScoringRuleDto> ScoringRules { get; init; } = [];
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

    public record PathogenDto
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public bool IsAtypical { get; init; }
        public ICollection<RiskFactorDto> RiskFactors { get; init; } = [];
    }

    public record RiskFactorDto
    {
        public string CriterionId { get; init; } = string.Empty;
        public int Priority { get; init; }
    }

    public record SuspectedCauseDto
    {
        public string PathogenId { get; init; } = string.Empty;
        public string Severity { get; init; } = string.Empty;
        public string TreatmentSite { get; init; } = string.Empty;
    }

    public record AntibioticGroupDto
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string? ParentId { get; init; }
    }

    public record AntibioticDto
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string AntibioticGroupId { get; init; } = string.Empty;
        public string Classification { get; init; } = string.Empty;
        public ICollection<string> PathogenIds { get; init; } = [];
        public ICollection<DosageDto> Dosages { get; init; } = [];
    }

    public record DosageDto
    {
        public string Id { get; init; } = string.Empty;
        public string RouteOfAdministration { get; init; } = string.Empty;
        public string Dose { get; init; } = string.Empty;
        public RangeDto? Crcl { get; init; }
    }

    public record RangeDto
    {
        public decimal Min { get; init; }
        public bool IsMinExclusive { get; init; }
        public decimal? Max { get; init; }
        public bool IsMaxExclusive { get; init; }
        public string? Unit { get; init; }
    }

    public record TreatmentDto
    {
        public string Id { get; init; } = string.Empty;
        public string Severity { get; init; } = string.Empty;
        public string TreatmentSite { get; init; } = string.Empty;
        public ICollection<ICollection<Guid>> MedicineIds { get; init; } = [];
        public ICollection<Guid> PathogenIds { get; init; } = [];
        public ICollection<Guid> CriteriaIds { get; init; } = [];
    }
}
