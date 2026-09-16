using System.Text.Json;
using System.Text.Json.Serialization;
using Respira.Domain.Entities;
using Respira.Domain.Enums;
using Respira.Domain.Models;

namespace Respira.Infrastructure.Util.Database
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
                var clinicalVar = JsonSerializer.Deserialize<ClinicalVariable>(
                    element.GetProperty("variable").GetRawText(), s_options)!;
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
    }
}
