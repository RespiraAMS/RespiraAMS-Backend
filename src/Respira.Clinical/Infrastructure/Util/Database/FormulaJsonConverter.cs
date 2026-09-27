using System.Text.Json;
using System.Text.Json.Serialization;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;

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
            if (element.TryGetProperty("variable", out _))
            {
                var variableElement = element.GetProperty("variable");
                var clinicalVar = (ClinicalVariable)JsonSerializer.Deserialize(
                    variableElement.GetRawText(),
                    ResolveClinicalVariableType(element),
                    s_options)!;
                return new VariableFormula(clinicalVar);
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
            }

            throw new JsonException("Unable to determine Formula type from JSON structure.");
        }

        /// <summary>
        /// ClinicalVariable is abstract, so System.Text.Json cannot deserialize into it directly.
        /// Resolve the concrete subtype from the embedded variable's own valueType, falling back
        /// to the owning formula node's resultType (variable nodes derive resultType 1:1 from
        /// ValueType: Boolean/Numeric map to themselves, String maps to Categorical).
        /// </summary>
        private static Type ResolveClinicalVariableType(JsonElement node)
        {
            var variableElement = node.GetProperty("variable");

            string? valueType = variableElement.TryGetProperty("valueType", out var vt)
                ? vt.GetString()
                : null;
            if (valueType is not null)
            {
                return valueType switch
                {
                    nameof(ClinicalValueType.Boolean) => typeof(BooleanClinicalVariable),
                    nameof(ClinicalValueType.Numeric) => typeof(NumericClinicalVariable),
                    nameof(ClinicalValueType.Categorical) => typeof(CategoricalClinicalVariable),
                    _ => throw new JsonException($"Unknown clinical variable valueType: {valueType}")
                };
            }

            string? resultType = node.TryGetProperty("resultType", out var rt)
                ? rt.GetString()
                : null;
            return resultType switch
            {
                nameof(ExpressionResultType.Boolean) => typeof(BooleanClinicalVariable),
                nameof(ExpressionResultType.Numeric) => typeof(NumericClinicalVariable),
                nameof(ExpressionResultType.String) => typeof(CategoricalClinicalVariable),
                _ => throw new JsonException(
                    "Unable to resolve concrete ClinicalVariable type: " +
                    $"missing valueType (got '{valueType ?? "<none>"}') and unsupported resultType (got '{resultType ?? "<none>"}')")
            };
        }
    }
}
