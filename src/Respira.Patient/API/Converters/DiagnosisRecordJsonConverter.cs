using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models;

namespace Respira.Patient.API.Converters
{
    /// <summary>
    /// This class is used to convert between <see cref="EmpiricalDiagnosisRecord"></see> and
    /// <see cref="TargetedDiagnosisRecord"></see> from JSON to CLR object without using a temporary
    /// DTO
    /// </summary>
    public class DiagnosisRecordJsonConverter : JsonConverter<DiagnosisRecord>
    {
        public override DiagnosisRecord? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            if (root.TryGetProperty("pathogen", out _))
                return root.Deserialize<TargetedDiagnosisRecord>(options);
            if (root.TryGetProperty("severity", out _)
                || root.TryGetProperty("treatmentSite", out _)
                || root.TryGetProperty("infectionProbabilityRecords", out _))
            {
                return root.Deserialize<EmpiricalDiagnosisRecord>(options);
            }

            return root.Deserialize<DiagnosisRecord>(options);
        }

        public override void Write(Utf8JsonWriter writer, DiagnosisRecord value, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
