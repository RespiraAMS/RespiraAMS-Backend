using System.Globalization;
using System.Text.Json;
using Domain.Enums;
using Domain.Models;

namespace Infrastructure.Data.Seeds;

public static class SeedDataLoader
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
        var antibioticGroups = dto.AntibioticGroups.Select(g => new AntibioticGroup
        {
            Id = GenerateId(g.Id),
            Name = g.Name,
            Description = g.Description,
            ParentId = ParseNullableId(g.ParentId),
        }).ToList();

        var pathogens = dto.Pathogens.Select(p => new Pathogen
        {
            Id = GenerateId(p.Id),
            Name = p.Name,
            Description = p.Description,
        }).ToList();

        var antibiotics = dto.Antibiotics.Select(a =>
        {
            var antibioticId = GenerateId(a.Id);
            var antibiotic = new Antibiotic
            {
                Id = antibioticId,
                Name = a.Name,
                AntibioticGroupId = ParseRequiredId(a.AntibioticGroupId, "antibiotic.antibioticGroupId"),
                Classification = ParseEnum(a.Category, AwareClassification.Unclassified),
                PathogenIds = a.PathogenIds.Select(ParseRequiredId).ToList(),
                Dosages = a.Dosages.Select(d => new Dosage
                {
                    Id = GenerateId(d.Id),
                    AntibioticId = antibioticId,
                    RouteOfAdministration = ParseEnum(d.RouteOfAdministration, RouteOfAdministration.Intravenous),
                    Dose = d.Dose,
                    GlomerularFiltrationRate = MapRange(d.GlomerularFiltrationRate),
                }).ToList(),
            };

            antibiotic.DosageIds = antibiotic.Dosages.Select(d => d.Id).ToList();

            return antibiotic;
        }).ToList();

        var criteria = new List<Criterion>();
        var criterionById = new Dictionary<Guid, Criterion>();
        var criterionByName = new Dictionary<string, Criterion>();

        Criterion AddCriterion(CriterionDto dtoCriterion)
        {
            if (!string.IsNullOrWhiteSpace(dtoCriterion.Name)
                && criterionByName.TryGetValue(dtoCriterion.Name, out var existing))
            {
                return existing;
            }

            var criterion = CreateCriterion(dtoCriterion);
            criteria.Add(criterion);
            criterionById[criterion.Id] = criterion;
            if (!string.IsNullOrWhiteSpace(dtoCriterion.Name))
            {
                criterionByName[dtoCriterion.Name] = criterion;
            }

            return criterion;
        }

        Criterion ResolveCriterion(CriterionDto? inline, string referenceId, string context)
        {
            if (inline is not null)
            {
                return AddCriterion(inline);
            }

            var id = ParseRequiredId(referenceId, $"{context}.criterionId");
            if (!criterionById.TryGetValue(id, out var criterion))
            {
                throw new InvalidOperationException(
                    $"Seed data: criterion '{referenceId}' referenced by {context} was not found in the criteria list.");
            }

            return criterion;
        }

        foreach (var criterion in dto.Criteria)
        {
            AddCriterion(criterion);
        }

        var diseases = dto.Diseases.Select(d =>
        {
            var diseaseId = GenerateId(d.Id);
            var disease = new Disease
            {
                Id = diseaseId,
                Name = d.Name,
                Description = d.Description,
                IcuScoreThreshold = d.IcuScoreThreshold,
                Causes = d.Causes.Select(c => new Cause
                {
                    Id = GenerateId(c.Id),
                    DiseaseId = diseaseId,
                    PathogenId = ParseRequiredId(c.PathogenId, "cause.pathogenId"),
                    Severity = ParseEnum(c.Severity, Severity.Mild),
                    TreatmentSite = ParseEnum(c.TreatmentSite, TreatmentSite.Outpatient),
                }).ToList(),
                IcuHospitalizeCriteria = d.IcuHospitalizeCriteria.Select(i => new IcuHospitalizeCriterion
                {
                    Id = GenerateId(i.Id),
                    DiseaseId = diseaseId,
                    CriterionId = ResolveCriterion(i.Criterion, i.CriterionId, "icuHospitalizeCriterion").Id,
                    Score = i.Score,
                }).ToList(),
                ResistanceRiskFactors = d.ResistanceRiskFactors.Select(r => new ResistanceRiskFactor
                {
                    Id = GenerateId(r.Id),
                    DiseaseId = diseaseId,
                    Name = r.Name,
                    CriterionId = ResolveCriterion(r.Criterion, r.CriterionId, "resistanceRiskFactor").Id,
                    PathogenId = ParseRequiredId(r.PathogenId, "resistanceRiskFactor.pathogenId"),
                }).ToList(),
                EmpiricTreatmentProtocols = d.EmpiricTreatmentProtocols.Select(p => new EmpiricTreatmentProtocol
                {
                    Id = GenerateId(p.Id),
                    DiseaseId = diseaseId,
                    Name = p.Name,
                    Issuer = p.Issuer,
                    IssueDate = ParseDate(p.IssueDate),
                    Version = p.Version,
                    Severity = ParseEnum(p.Severity, Severity.Mild),
                    TreatmentSite = ParseEnum(p.TreatmentSite, TreatmentSite.Outpatient),
                    SpecialInfectionId = ParseNullableId(p.SpecialInfectionId),
                    OtherCriteriaIds = p.OtherCriteria
                        .Select(c => ResolveCriterion(c, string.Empty, "empiricTreatmentProtocol.otherCriteria").Id)
                        .Concat(p.OtherCriteriaIds
                            .Select(id => ResolveCriterion(null, id, "empiricTreatmentProtocol.otherCriteria").Id))
                        .ToList(),
                    MedicineIds = p.MedicineIds.Select(ParseRequiredId).ToList(),
                }).ToList(),
            };

            return disease;
        }).ToList();

        var antibiograms = dto.Antibiograms.Select(a => new Antibiogram
        {
            Id = GenerateId(a.Id),
            PathogenId = ParseRequiredId(a.PathogenId, "antibiogram.pathogenId"),
            MicLevel = ParseEnum(a.MicLevel, MinimumInhibitoryConcentration.Susceptible),
            MicIds = a.MicIds.Select(ParseRequiredId).ToList(),
            FirstPriorityMedicineIds = a.FirstPriorityMedicineIds.Select(ParseRequiredId).ToList(),
            SecondPriorityMedicineIds = a.SecondPriorityMedicineIds.Select(ParseRequiredId).ToList(),
        }).ToList();

        return new SeedData
        {
            AntibioticGroups = antibioticGroups,
            Pathogens = pathogens,
            Antibiotics = antibiotics,
            Criteria = criteria,
            Diseases = diseases,
            Antibiograms = antibiograms,
        };
    }

    private static Criterion CreateCriterion(CriterionDto dto)
    {
        return dto.Type.Trim().ToLowerInvariant() switch
        {
            "boolean" => new BooleanCriterion
            {
                Id = GenerateId(dto.Id),
                Name = dto.Name,
            },
            "numeric" => new NumericCriterion
            {
                Id = GenerateId(dto.Id),
                Name = dto.Name,
                Value = MapRange(dto.Value) ?? throw new InvalidOperationException(
                    $"Seed data: numeric criterion '{dto.Name}' requires a value."),
            },
            _ => throw new InvalidOperationException($"Seed data: unsupported criterion type '{dto.Type}'."),
        };
    }

    private static Guid GenerateId(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? Guid.CreateVersion7() : Guid.Parse(id);
    }

    private static Guid ParseRequiredId(string? id)
    {
        return Guid.TryParse(id, out var result)
            ? result
            : throw new InvalidOperationException($"Seed data: '{id}' is not a valid id.");
    }

    private static Guid ParseRequiredId(string? id, string field)
    {
        return Guid.TryParse(id, out var result)
            ? result
            : throw new InvalidOperationException($"Seed data: '{field}' must reference a valid id, got '{id}'.");
    }

    private static Guid? ParseNullableId(string? id)
    {
        return string.IsNullOrWhiteSpace(id) ? null : Guid.Parse(id);
    }

    private static T ParseEnum<T>(string value, T fallback) where T : struct, Enum
    {
        return Enum.TryParse(value, true, out T result) ? result : fallback;
    }

    private static DateOnly ParseDate(string value)
    {
        return DateOnly.TryParse(value, CultureInfo.InvariantCulture, out var result) ? result : default;
    }

    private static Domain.Models.Range? MapRange(RangeDto? dto)
    {
        if (dto is null)
        {
            return null;
        }

        return new Domain.Models.Range
        {
            Min = dto.Min,
            IsMinExclusive = dto.IsMinExclusive,
            Max = dto.Max ?? decimal.MaxValue,
            IsMaxExclusive = dto.IsMaxExclusive,
            Unit = dto.Unit,
        };
    }
}

public class SeedData
{
    public required List<AntibioticGroup> AntibioticGroups { get; init; }
    public required List<Pathogen> Pathogens { get; init; }
    public required List<Antibiotic> Antibiotics { get; init; }
    public required List<Criterion> Criteria { get; init; }
    public required List<Disease> Diseases { get; init; }
    public required List<Antibiogram> Antibiograms { get; init; }
}
