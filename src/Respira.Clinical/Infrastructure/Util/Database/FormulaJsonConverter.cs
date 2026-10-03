using System.Text.Json;
using System.Text.Json.Serialization;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Clinical.Infrastructure.Util.Database
{
    /// <summary>
    /// Handles serialization/deserialization of <see cref="Formula"/> trees.
    /// Uses manual tree construction to support both legacy JSON (no $type discriminator)
    /// and modern JSON (with $type discriminator), without registering a converter in
    /// JsonSerializerOptions which would break the [JsonPolymorphic] metadata pipeline in .NET 8+.
    /// </summary>
    internal static class FormulaSerializer
    {
        private static readonly JsonSerializerOptions s_options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        public static string Serialize(Formula formula)
        {
            return JsonSerializer.Serialize(formula, s_options);
        }

        public static Formula Deserialize(string json)
        {
            using var doc = JsonDocument.Parse(json);
            return BuildFormula(doc.RootElement);
        }

        private static Formula BuildFormula(JsonElement element)
        {
            if (element.TryGetProperty("variable", out var variableElement))
            {
                return new VariableFormula(BuildClinicalVariable(variableElement, element));
            }

            if (element.TryGetProperty("formula", out var innerEl))
            {
                return new UnaryFormula(BuildFormula(innerEl));
            }

            if (element.TryGetProperty("left", out var leftEl) &&
                element.TryGetProperty("right", out var rightEl))
            {
                var left = BuildFormula(leftEl);
                var right = BuildFormula(rightEl);
                var op = JsonSerializer.Deserialize<ExpressionOperator>(
                    element.GetProperty("operator").GetRawText(), s_options);
                return new BinaryFormula(left, right, op);
            }

            if (element.TryGetProperty("condition", out var condEl))
            {
                var condition = BuildFormula(condEl);
                var ifTrue = BuildFormula(element.GetProperty("ifTrue"));
                var ifFalse = BuildFormula(element.GetProperty("ifFalse"));
                return new TernaryFormula(condition, ifTrue, ifFalse);
            }

            if (element.TryGetProperty("constant", out var constEl))
            {
                if (constEl.ValueKind == JsonValueKind.Number)
                {
                    return new NumericConstantFormula(constEl.GetDecimal());
                }

                if (constEl.ValueKind is JsonValueKind.True or JsonValueKind.False)
                {
                    return new BooleanConstantFormula(constEl.GetBoolean());
                }

                if (constEl.ValueKind == JsonValueKind.String)
                {
                    return new CategoricalConstantFormula(constEl.GetString() ?? string.Empty);
                }
            }

            throw new JsonException("Unable to determine Formula type from JSON structure.");
        }

        /// <summary>
        /// Builds the clinical variable embedded in a formula node.
        /// <para>
        /// <see cref="VariableFormula.Variable"/> is declared as the abstract <see cref="ClinicalVariable"/>,
        /// so System.Text.Json serializes base-type members only: subtype members
        /// (<c>AcceptedRange</c>, <c>NormalRange</c>, <c>AcceptedValues</c>) never reach the database.
        /// Those same members are 'required' on the entity, so deserializing the stored node directly
        /// throws "missing required properties including: 'acceptedRange'". Read through a lenient DTO
        /// instead; formula evaluation only relies on Id, Code and ValueType.
        /// </para>
        /// </summary>
        private static ClinicalVariable BuildClinicalVariable(JsonElement variableElement, JsonElement node)
        {
            var json = JsonSerializer.Deserialize<VariableJson>(variableElement.GetRawText(), s_options)!;

            var code = json.Code ?? string.Empty;
            var name = json.Name ?? string.Empty;
            var description = json.Description ?? string.Empty;

            return ResolveClinicalVariableType(variableElement, node) switch
            {
                ClinicalValueType.Numeric => new NumericClinicalVariable
                {
                    Id = json.Id,
                    Code = code,
                    Name = name,
                    Description = description,
                    IsRequired = json.IsRequired,
                    Category = json.Category,
                    CanonicalUnit = json.CanonicalUnit,
                    // Missing range: stand-in only, so IsValidValue cannot NRE. Validation always
                    // runs against the real clinical_variables row, never against formula-embedded copies.
                    AcceptedRange = json.AcceptedRange?.ToRange() ?? PermissiveRange(json.CanonicalUnit),
                    NormalRange = json.NormalRange?.ToRange(),
                },
                ClinicalValueType.Categorical => new CategoricalClinicalVariable(json.AcceptedValues ?? [])
                {
                    Id = json.Id,
                    Code = code,
                    Name = name,
                    Description = description,
                    IsRequired = json.IsRequired,
                    Category = json.Category,
                    CanonicalUnit = json.CanonicalUnit,
                },
                _ => new BooleanClinicalVariable
                {
                    Id = json.Id,
                    Code = code,
                    Name = name,
                    Description = description,
                    IsRequired = json.IsRequired,
                    Category = json.Category,
                    CanonicalUnit = json.CanonicalUnit,
                },
            };
        }

        private static Range PermissiveRange(string? unit) => new()
        {
            Min = decimal.MinValue,
            IsMinExclusive = false,
            Max = decimal.MaxValue,
            IsMaxExclusive = false,
            Unit = unit,
        };

        /// <summary>
        /// Lenient projection of the variable node embedded in a stored formula: every member is
        /// optional so partial/legacy rows deserialize instead of throwing.
        /// </summary>
        private sealed class VariableJson
        {
            public Guid Id { get; init; }
            public string? Code { get; init; }
            public string? Name { get; init; }
            public string? Description { get; init; }
            public bool IsRequired { get; init; }
            public ClinicalVariableCategory Category { get; init; }
            public string? CanonicalUnit { get; init; }
            public RangeJson? AcceptedRange { get; init; }
            public RangeJson? NormalRange { get; init; }
            public List<string>? AcceptedValues { get; init; }
        }

        private sealed class RangeJson
        {
            public decimal? Min { get; init; }
            public decimal? Max { get; init; }
            public bool? IsMinExclusive { get; init; }
            public bool? IsMaxExclusive { get; init; }
            public string? Unit { get; init; }

            public Range ToRange() => new()
            {
                Min = Min ?? decimal.MinValue,
                Max = Max ?? decimal.MaxValue,
                IsMinExclusive = IsMinExclusive ?? false,
                IsMaxExclusive = IsMaxExclusive ?? false,
                Unit = Unit,
            };
        }

        /// <summary>
        /// Resolve the concrete subtype of the embedded variable from its own valueType, falling
        /// back to the owning formula node's resultType (variable nodes derive valueType 1:1 from
        /// resultType: Boolean/Numeric map to themselves, String maps to Categorical).
        /// </summary>
        private static ClinicalValueType ResolveClinicalVariableType(JsonElement variableElement, JsonElement node)
        {
            string? valueType = variableElement.TryGetProperty("valueType", out var vt)
                ? vt.GetString()
                : null;
            if (valueType is not null)
            {
                return valueType switch
                {
                    nameof(ClinicalValueType.Boolean) => ClinicalValueType.Boolean,
                    nameof(ClinicalValueType.Numeric) => ClinicalValueType.Numeric,
                    nameof(ClinicalValueType.Categorical) => ClinicalValueType.Categorical,
                    _ => throw new JsonException($"Unknown clinical variable valueType: {valueType}")
                };
            }

            string? resultType = node.TryGetProperty("resultType", out var rt)
                ? rt.GetString()
                : null;
            return resultType switch
            {
                nameof(ExpressionResultType.Boolean) => ClinicalValueType.Boolean,
                nameof(ExpressionResultType.Numeric) => ClinicalValueType.Numeric,
                nameof(ExpressionResultType.String) => ClinicalValueType.Categorical,
                _ => throw new JsonException(
                    "Unable to resolve concrete ClinicalVariable type: " +
                    $"missing valueType (got '{valueType ?? "<none>"}') and unsupported resultType (got '{resultType ?? "<none>"}')")
            };
        }
    }
}
