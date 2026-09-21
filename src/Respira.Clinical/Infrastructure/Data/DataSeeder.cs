using System.Text.Json;
using ImTools;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;

namespace Respira.Clinical.Infrastructure.Data
{
    public class SeedData
    {
        public required ICollection<ClinicalVariable> ClinicalVariables { get; init; }
        public required ICollection<Criterion> Criteria { get; init; }
        public required ICollection<ScoreMetrics> ScoreMetrics { get; init; }
        public required ICollection<Pathogen> Pathogens { get; init; }
        public required ICollection<RiskFactor> RiskFactors { get; init; }
        public required ICollection<SuspectedCause> SuspectedCauses { get; init; }
        public required ICollection<AntibioticGroup> AntibioticGroups { get; init; }
        public required ICollection<Antibiotic> Antibiotics { get; init; }
        public required ICollection<Treatment> Treatments { get; init; }
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
            var variables = dto.ClinicalVariables.Select(v =>
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
            }).ToList();

            var variableLookup = variables.ToDictionary(v => v.Id);

            var criteria = dto.Criteria.Select(c =>
            {
                var criterionId = GenerateId(c.Id);
                var formula = MapFormula(c.Formula, variableLookup);
                return new Criterion(c.Name, formula)
                {
                    Id = criterionId,
                };
            }).ToList();

            var criterionLookup = criteria.ToDictionary(c => c.Id);

            var scoreMetrics = dto.ScoreMetrics.Select(s =>
            {
                var metricId = GenerateId(s.Id);
                var scoringRules = s.ScoringRules.Select(r =>
                {
                    var ruleId = GenerateId(r.Id);
                    var criterionId = GenerateId(r.CriterionId);
                    var criterion = criterionLookup[criterionId];
                    var scoreFunction = MapFormula(r.ScoreFunction, variableLookup);
                    return new ScoringRule
                    {
                        Id = ruleId,
                        ScoreMetricsId = metricId,
                        CriterionId = criterionId,
                        Criterion = criterion,
                        ScoreFunction = scoreFunction,
                    };
                });

                return new ScoreMetrics
                {
                    Id = metricId,
                    Name = s.Name,
                    Code = s.Code,
                    Description = s.Description,
                    ScoringRules = [.. scoringRules],
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
                        Priority = rf.Priority,
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


            var antibiotics = dto.Antibiotics.Select(a =>
            {
                var antibioticId = GenerateId(a.Id);
                return new Antibiotic
                {
                    Id = antibioticId,
                    Name = a.Name,
                    AntibioticGroupId = ParseRequiredId(a.AntibioticGroupId, "antibiotic.antibioticGroupId"),
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
                var pathogens = t.PathogenIds.Select(p => pathogenLookup[p]);
                var criteria = t.CriteriaIds.Select(c => criterionLookup[c]);
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
                    Pathogens = [.. pathogens],
                    Criteria = [.. criteria],
                    Medicines = [.. medicines],
                };
            }).ToList();


            return new SeedData
            {
                ClinicalVariables = [.. variables],
                Criteria = [.. criteria],
                ScoreMetrics = [.. scoreMetrics],
                Pathogens = [.. pathogens],
                RiskFactors = allRiskFactors,
                SuspectedCauses = [.. suspectedCauses],
                AntibioticGroups = [.. antibioticGroups],
                Antibiotics = [.. antibiotics],
                Treatments = [.. treatments],
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
                var op = ParseEnum(dto.Operator, ExpressionOperator.ADD);
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

            throw new ArgumentException("Invalid formula DTO: unable to determine formula type");
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
}
