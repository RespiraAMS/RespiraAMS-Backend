using System.Text.Json;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Clinical.Infrastructure.Data
{
    public class SeedData
    {
        public required ICollection<ClinicalVariable> ClinicalVariables { get; init; }
        public required ICollection<Criterion> Criteria { get; init; }
        public required ICollection<ClinicalMetric> ClinicalMetrics { get; init; }
        public required ICollection<Pathogen> Pathogens { get; init; }
        public required ICollection<RiskFactor> RiskFactors { get; init; }
        public required ICollection<SuspectedCause> SuspectedCauses { get; init; }
        public required ICollection<AntibioticGroup> AntibioticGroups { get; init; }
        public required ICollection<Antibiotic> Antibiotics { get; init; }
        public required ICollection<Treatment> Treatments { get; init; }
        public required ICollection<Antibiogram> Antibiograms { get; init; }
    }

    public static class DataSeeder
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        public static SeedData Load(string filePath)
        {
            var path = Path.IsPathRooted(filePath)
                ? filePath
                : Path.Combine(AppContext.BaseDirectory, filePath);

            var json = File.ReadAllText(path);

            var dto = JsonSerializer.Deserialize<SeedDataDto>(json, JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize seed data.");

            return MapToDomain(dto);
        }

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
            var variables = dto.ClinicalVariables.Select<ClinicalVariableDto, ClinicalVariable>(v =>
            {
                var variableId = GenerateId(v.Id);
                if (v.AcceptedRange is not null && v.AcceptedValues.Count > 0)
                {
                    throw new InvalidOperationException("Seed data: accepted range and accepted values are mutually exclusive");
                }

                if (v.AcceptedRange is not null)
                {
                    return new NumericClinicalVariable
                    {
                        Id = variableId,
                        Name = v.Name,
                        Code = v.Code,
                        Description = v.Description,
                        CanonicalUnit = v.CanonicalUnit,
                        IsRequired = v.IsRequired,
                        Category = ParseEnum(v.Category, ClinicalVariableCategory.PersonalInformation),
                        AcceptedRange = MapRange(v.AcceptedRange)!,
                        NormalRange = MapRange(v.NormalRange),
                    };
                }

                if (v.AcceptedValues.Count > 0)
                {
                    return new CategoricalClinicalVariable
                    {
                        Id = variableId,
                        Name = v.Name,
                        Code = v.Code,
                        Description = v.Description,
                        CanonicalUnit = v.CanonicalUnit,
                        IsRequired = v.IsRequired,
                        Category = ParseEnum(v.Category, ClinicalVariableCategory.PersonalInformation),
                        AcceptedValues = [.. v.AcceptedValues],
                    };
                }

                return new BooleanClinicalVariable
                {
                    Id = variableId,
                    Name = v.Name,
                    Code = v.Code,
                    Description = v.Description,
                    CanonicalUnit = v.CanonicalUnit,
                    IsRequired = v.IsRequired,
                    Category = ParseEnum(v.Category, ClinicalVariableCategory.PersonalInformation),
                };
            }).ToList();

            var variableLookup = variables.ToDictionary(v => v.Id);

            // Prerequisite formulas reference other variables, so they are mapped in a second
            // pass once every variable entity exists. Pair DTOs with entities by position:
            // re-deriving the id with GenerateId would mint a new Guid for blank DTO ids.
            foreach (var (variableDto, variable) in dto.ClinicalVariables.Zip(variables))
            {
                if (variableDto.Prerequisite is null)
                {
                    continue;
                }

                variable.Prerequisite = MapFormula(variableDto.Prerequisite, variableLookup);
            }

            var criteria = dto.Criteria.Select(c =>
            {
                try
                {
                    var criterionId = GenerateId(c.Id);
                    var formula = MapFormula(c.Formula, variableLookup);
                    return new Criterion()
                    {
                        Id = criterionId,
                        Name = c.Name,
                        Formula = formula,
                    };
                }
                catch (ArgumentException)
                {
                    Console.WriteLine($"Criterion {c.Id} has invalid formula: {c.Formula}");
                    throw;
                }

            }).ToList();

            var criterionLookup = criteria.ToDictionary(c => c.Id);

            var clinicalMetrics = dto.ClinicalMetrics.Select(s =>
            {
                var metricId = GenerateId(s.Id);
                var scoringRules = s.Rules.Select<MetricRuleDto, MetricRule>(r =>
                {
                    try
                    {
                        var ruleId = GenerateId(r.Id);
                        var criterionId = GenerateId(r.CriterionId);
                        var criterion = criterionLookup[criterionId];

                        // If score function provided, return ScoringRule
                        if (r.ScoreFunction is not null)
                        {
                            var scoreFunction = MapFormula(r.ScoreFunction, variableLookup);
                            return new ScoringRule
                            {
                                Id = ruleId,
                                ClinicalMetricId = metricId,
                                CriterionId = criterionId,
                                Criterion = criterion,
                                ScoreFunction = scoreFunction,
                            };
                        }

                        if (r.IsMajor is not null)
                        {
                            return new MajorMinorRule
                            {
                                Id = ruleId,
                                ClinicalMetricId = metricId,
                                CriterionId = criterionId,
                                Criterion = criterion,
                                IsMajor = r.IsMajor.Value,
                            };
                        }

                        throw new ArgumentException("Invalid rule type for metrics rule: either score function or is major be provided");
                    }
                    catch (ArgumentException)
                    {
                        Console.WriteLine($"Criterion {r.CriterionId} has invalid formula: {r.ScoreFunction}");
                        throw;
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"Criterion {r.CriterionId} has invalid formula: {r.ScoreFunction}");
                        throw;
                    }
                });

                return new ClinicalMetric
                {
                    Id = metricId,
                    Name = s.Name,
                    Code = s.Code,
                    Description = s.Description,
                    Version = s.Version,
                    Publisher = s.Publisher,
                    IsApplied = s.IsApplied,
                    Rules = [.. scoringRules],
                };
            }).ToList();

            var allRiskFactors = new List<RiskFactor>();
            var pathogens = dto.Pathogens.Select(p =>
            {
                var pathogenId = GenerateId(p.Id);
                var riskFactors = p.RiskFactors.Select(rf =>
                {
                    var criterionId = GenerateId(rf.CriterionId);
                    var criterion = criterionLookup[criterionId];
                    var riskFactor = new RiskFactor
                    {
                        PathogenId = pathogenId,
                        CriterionId = criterionId,
                        Criterion = criterion,
                        IsCriticalFactor = rf.IsCriticalFactor,
                    };
                    allRiskFactors.Add(riskFactor);
                    return riskFactor;
                });

                return new Pathogen
                {
                    Id = pathogenId,
                    Name = p.Name,
                    Description = p.Description,
                    IsAtypical = p.IsAtypical,
                    RiskFactors = [.. riskFactors],
                };
            }).ToList();

            var pathogenLookup = pathogens.ToDictionary(p => p.Id);

            var suspectedCauses = dto.SuspectedCauses.Select(sc =>
            {
                var pathogenId = GenerateId(sc.PathogenId);
                var pathogen = pathogenLookup[pathogenId];
                return new SuspectedCause
                {
                    PathogenId = pathogenId,
                    Pathogen = pathogen,
                    Severity = ParseEnum(sc.Severity, Severity.Mild),
                    TreatmentSite = ParseEnum(sc.TreatmentSite, TreatmentSite.Outpatient),
                };
            });

            var antibioticGroups = dto.AntibioticGroups.Select(g => new AntibioticGroup
            {
                Id = GenerateId(g.Id),
                Name = g.Name,
                Description = g.Description,
                ParentId = ParseNullableId(g.ParentId),
            }).ToList();

            var antibioticGroupLookup = antibioticGroups.ToDictionary(v => v.Id);

            // Antibiotic group also have self reference is parent, so we need to map them on a second time
            foreach (var (antibioticGroupDto, antibioticGroup) in dto.AntibioticGroups.Zip(antibioticGroups))
            {
                if (antibioticGroupDto.ParentId is not null)
                {
                    var parentId = GenerateId(antibioticGroupDto.ParentId);
                    antibioticGroup.ParentId = parentId;
                    antibioticGroup.Parent = antibioticGroupLookup[parentId];
                }
            }

            var antibiotics = dto.Antibiotics.Select(a =>
            {
                var antibioticId = GenerateId(a.Id);
                var antibioticGroupId = ParseRequiredId(a.AntibioticGroupId, "antibiotic.antibioticGroupId");
                return new Antibiotic
                {
                    Id = antibioticId,
                    Name = a.Name,
                    AntibioticGroupId = antibioticGroupId,
                    AntibioticGroup = antibioticGroupLookup[antibioticGroupId],
                    Classification = ParseEnum(a.Classification, AwareClassification.Unclassified),
                    Dosages = [.. a.Dosages.Select(d => new Dosage
                    {
                        Id = GenerateId(d.Id),
                        AntibioticId = antibioticId,
                        RouteOfAdministration = ParseEnum(d.RouteOfAdministration, RouteOfAdministration.Intravenous),
                        Dose = d.Dose,
                        Crcl = MapRange(d.Crcl),
                    })],
                };
            }).ToList();

            var treatments = dto.Treatments.Select(t =>
            {
                var treatmentId = GenerateId(t.Id);
                var pathogenIds = t.PathogenIds.Select(p => pathogenLookup[p]);
                var criterionIds = t.CriteriaIds.Select(c => criterionLookup[c]);
                var medicines = t.MedicineIds
                    .Select(composition => new MedicineComposition
                    {
                        TreatmentId = treatmentId,
                        Antibiotics = [.. composition.Select(m => antibiotics.First(a => a.Id == m))]
                    })
                    .ToList();

                return new Treatment
                {
                    Id = treatmentId,
                    Severity = ParseEnum(t.Severity, Severity.Mild),
                    TreatmentSite = ParseEnum(t.TreatmentSite, TreatmentSite.Outpatient),
                    Pathogens = [.. pathogenIds],
                    Criteria = [.. criterionIds],
                    Medicines = [.. medicines],
                };
            }).ToList();

            var antibiograms = dto.Antibiograms.Select(a =>
            {
                var category = ParseEnum(a.MicCategory, MicCategory.Susceptible);
                var pathogenId = ParseRequiredId(a.PathogenId, "pathogenId");
                var pathogen = pathogenLookup[pathogenId];

                return new Antibiogram
                {
                    MicCategory = category,
                    PathogenId = pathogenId,
                    Pathogen = pathogen,
                    Antibiotics = a.AntibioticIds
                        .ConvertAll(ai => antibiotics.First(x => x.Id == ParseRequiredId(ai, "antibioticIds"))),
                };
            }).ToList();


            return new SeedData
            {
                ClinicalVariables = [.. variables],
                Criteria = [.. criteria],
                ClinicalMetrics = [.. clinicalMetrics],
                Pathogens = [.. pathogens],
                RiskFactors = allRiskFactors,
                SuspectedCauses = [.. suspectedCauses],
                AntibioticGroups = [.. antibioticGroups],
                Antibiotics = [.. antibiotics],
                Treatments = [.. treatments],
                Antibiograms = [.. antibiograms],
            };
        }

        private static Formula MapFormula(FormulaDto dto, Dictionary<Guid, ClinicalVariable> variableLookup)
        {
            if (dto.Constant.HasValue)
            {
                var element = dto.Constant.Value;
                if (dto.ResultType == "Boolean")
                {
                    return new BooleanConstantFormula(element.GetBoolean());
                }
                if (dto.ResultType == "String")
                {
                    return new CategoricalConstantFormula(element.GetString() ?? "");
                }
                return new NumericConstantFormula(element.GetDecimal());
            }

            if (dto.Variable is not null)
            {
                var variableId = GenerateId(dto.Variable.Id);
                var variable = variableLookup[variableId];
                return new VariableFormula(variable);
            }

            if (dto.Operator is not null && dto.Left is not null && dto.Right is not null)
            {
                var left = MapFormula(dto.Left, variableLookup);
                var right = MapFormula(dto.Right, variableLookup);
                var op = ParseEnum(dto.Operator, ExpressionOperator.Add);
                return new BinaryFormula(left, right, op);
            }

            if (dto.Operand is not null)
            {
                var operand = MapFormula(dto.Operand, variableLookup);
                return new UnaryFormula(operand);
            }

            if (dto.Condition is not null && dto.IfTrue is not null && dto.IfFalse is not null)
            {
                var condition = MapFormula(dto.Condition, variableLookup);
                var ifTrue = MapFormula(dto.IfTrue, variableLookup);
                var ifFalse = MapFormula(dto.IfFalse, variableLookup);
                return new TernaryFormula(condition, ifTrue, ifFalse);
            }

            throw new ArgumentException("Invalid formula DTO: unable to determine formula type: {dto}", JsonSerializer.Serialize(dto));
        }

        private static Guid GenerateId(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? Guid.CreateVersion7() : Guid.Parse(id);
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

        private static Range? MapRange(RangeDto? dto)
        {
            if (dto is null)
            {
                return null;
            }

            return new Range
            {
                Min = dto.Min,
                IsMinExclusive = dto.IsMinExclusive,
                Max = dto.Max ?? decimal.MaxValue,
                IsMaxExclusive = dto.IsMaxExclusive,
                Unit = dto.Unit,
            };
        }
    }
}
