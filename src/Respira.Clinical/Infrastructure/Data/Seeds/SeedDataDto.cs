namespace Infrastructure.Data.Seeds;

public record SeedDataDto
{
    public List<AntibioticGroupDto> AntibioticGroups { get; init; } = [];
    public List<PathogenDto> Pathogens { get; init; } = [];
    public List<AntibioticDto> Antibiotics { get; init; } = [];
    public List<AntibiogramDto> Antibiograms { get; init; } = [];
    public List<CriterionDto> Criteria { get; init; } = [];
    public List<DiseaseDto> Diseases { get; init; } = [];
}

public record AntibioticGroupDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ParentId { get; init; }
}

public record PathogenDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public record AntibioticDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string AntibioticGroupId { get; init; } = string.Empty;
    public string Classification { get; init; } = string.Empty;
    public List<string> PathogenIds { get; init; } = [];
    public List<DosageDto> Dosages { get; init; } = [];
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

public record AntibiogramDto
{
    public string Id { get; init; } = string.Empty;
    public string PathogenId { get; init; } = string.Empty;
    public string MicLevel { get; init; } = string.Empty;
    public List<string> MicIds { get; init; } = [];
    public List<string> FirstPriorityMedicineIds { get; init; } = [];
    public List<string> SecondPriorityMedicineIds { get; init; } = [];
}

public record CriterionDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public RangeDto? Value { get; init; }
}

public record DiseaseDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int IcuScoreThreshold { get; init; }
    public List<CauseDto> Causes { get; init; } = [];
    public List<IcuHospitalizeCriterionDto> IcuHospitalizeCriteria { get; init; } = [];
    public List<ResistanceRiskFactorDto> ResistanceRiskFactors { get; init; } = [];
    public List<EmpiricTreatmentProtocolDto> EmpiricTreatmentProtocols { get; init; } = [];
}

public record CauseDto
{
    public string Id { get; init; } = string.Empty;
    public string PathogenId { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string TreatmentSite { get; init; } = string.Empty;
}

public record IcuHospitalizeCriterionDto
{
    public string Id { get; init; } = string.Empty;
    public CriterionDto? Criterion { get; init; }
    public string CriterionId { get; init; } = string.Empty;
    public int Score { get; init; }
}

public record ResistanceRiskFactorDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public CriterionDto? Criterion { get; init; }
    public string CriterionId { get; init; } = string.Empty;
    public string PathogenId { get; init; } = string.Empty;
}

public record EmpiricTreatmentProtocolDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string IssueDate { get; init; } = string.Empty;
    public int Version { get; init; }
    public string Severity { get; init; } = string.Empty;
    public string TreatmentSite { get; init; } = string.Empty;
    public string? SpecialInfectionId { get; init; }
    public List<string> OtherCriteriaIds { get; init; } = [];
    public List<CriterionDto> OtherCriteria { get; init; } = [];
    public List<string> MedicineIds { get; init; } = [];
}
