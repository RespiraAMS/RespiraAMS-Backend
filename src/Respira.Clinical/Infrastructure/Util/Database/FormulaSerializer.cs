using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Respira.Clinical.Domain.Models;

namespace Respira.Clinical.Infrastructure.Util.Database
{
    /// <summary>
    /// Handles serialization/deserialization of <see cref="Formula"/> trees stored in jsonb columns.
    /// <para>
    /// Round-trip is fully driven by the <c>[JsonPolymorphic]</c>/<c>[JsonDerivedType]</c> metadata on
    /// <see cref="Formula"/> (every node carries a <c>$type</c> discriminator) and by the stable
    /// <see cref="VariableRef"/> payload of variable nodes, so no manual tree construction is needed.
    /// </para>
    /// <para>
    /// The stored document cannot be handed to System.Text.Json as-is: postgres <c>jsonb</c> does not
    /// preserve key order (it sorts keys by length, then bytewise), so e.g. a binary node comes back as
    /// <c>{"left": ..., "$type": "binary", ...}</c>. System.Text.Json only looks for the discriminator
    /// as the *first* property of an object, and throws "must specify a type discriminator" otherwise.
    /// <see cref="MoveDiscriminatorsFirst"/> rewrites the parsed tree with <c>$type</c> leading every
    /// object before deserializing.
    /// </para>
    /// </summary>
    public static class FormulaSerializer
    {
        private const string TypeDiscriminator = "$type";

        private static readonly JsonSerializerOptions s_options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        public static string Serialize(Formula formula)
        {
            // The type argument must be Formula (not the runtime type) so the root node
            // is written through the polymorphic pipeline and gets its own $type.
            return JsonSerializer.Serialize(formula, s_options);
        }

        public static Formula Deserialize(string json)
        {
            return JsonSerializer.Deserialize<Formula>(MoveDiscriminatorsFirst(json), s_options)
                ?? throw new JsonException("Formula JSON payload deserialized to null.");
        }

        /// <summary>
        /// Rewrites the JSON so every object that carries a <c>$type</c> discriminator lists it first,
        /// recursively (nested nodes are reordered by jsonb too).
        /// </summary>
        private static string MoveDiscriminatorsFirst(string json)
        {
            using var doc = JsonDocument.Parse(json);

            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(buffer))
            {
                WriteNormalized(doc.RootElement, writer);
            }

            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }

        private static void WriteNormalized(JsonElement element, Utf8JsonWriter writer)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    writer.WriteStartObject();
                    if (element.TryGetProperty(TypeDiscriminator, out var discriminator))
                    {
                        writer.WritePropertyName(TypeDiscriminator);
                        WriteNormalized(discriminator, writer);
                    }

                    foreach (var property in element.EnumerateObject())
                    {
                        if (property.Name == TypeDiscriminator) continue;
                        writer.WritePropertyName(property.Name);
                        WriteNormalized(property.Value, writer);
                    }

                    writer.WriteEndObject();
                    break;
                case JsonValueKind.Array:
                    writer.WriteStartArray();
                    foreach (var item in element.EnumerateArray())
                    {
                        WriteNormalized(item, writer);
                    }

                    writer.WriteEndArray();
                    break;
                default:
                    element.WriteTo(writer);
                    break;
            }
        }
    }
}
