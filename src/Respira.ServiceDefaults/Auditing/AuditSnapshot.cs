using System.Buffers;
using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Respira.ServiceDefaults.Auditing;

/// <summary>
/// Creates and applies JSON snapshots of tracked entities.
///
/// The snapshot contains mapped scalar properties and owned (JSON) navigations.
/// Non-owned collections are recorded as id references under <c>$refs</c> so a
/// preview can show relationship changes without pulling the whole graph into the
/// audit row. Many-to-many join rows are captured separately as column/value
/// objects by <see cref="ToJoinJson"/>.
/// </summary>
public static class AuditSnapshot
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters =
        {
            new JsonStringEnumConverter(),
            new MicrosecondDateTimeOffsetConverter(),
            new MicrosecondDateTimeConverter(),
        },
    };

    public static string ToJson(EntityEntry entry, bool original = false)
    {
        try
        {
            var snapshot = Create(entry, original);
            return JsonSerializer.Serialize(snapshot, Options);
        }
        catch
        {
            // Auditing must never make a business transaction fail.
            return "{}";
        }
    }

    /// <summary>
    /// Serializes a many-to-many join row as <c>{ "column": value }</c>.
    /// </summary>
    public static string ToJoinJson(EntityEntry entry, bool original = false)
    {
        try
        {
            var snapshot = new Dictionary<string, object?>(StringComparer.Ordinal);

            foreach (var property in entry.Properties)
            {
                var column = property.Metadata.GetColumnName();
                if (string.IsNullOrWhiteSpace(column))
                    column = property.Metadata.Name;

                snapshot[column!] = original ? property.OriginalValue : property.CurrentValue;
            }

            return JsonSerializer.Serialize(snapshot, Options);
        }
        catch
        {
            return "{}";
        }
    }

    public static IReadOnlyList<string> Apply(
        object entity,
        string snapshotJson,
        JsonSerializerOptions? options = null,
        IReadOnlySet<string>? mutateInsteadOfReplace = null
    )
    {
        ArgumentNullException.ThrowIfNull(entity);

        using var document = JsonDocument.Parse(snapshotJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Audit snapshot must be a JSON object");

        var applied = new List<string>();
        var serializerOptions = options ?? Options;

        foreach (var propertyJson in document.RootElement.EnumerateObject())
        {
            if (propertyJson.Name.StartsWith('$'))
                continue;

            var property = entity
                .GetType()
                .GetProperty(
                    propertyJson.Name,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase
                );

            if (property?.CanWrite != true)
                continue;

            var targetType = ResolveTargetType(property.PropertyType);
            var currentValue = property.GetValue(entity);

            // Owned objects are tracked by EF as separate entries. Mutating the
            // existing instance lets EF detect the change without replacing a
            // tracked owned entity.
            if (
                mutateInsteadOfReplace?.Contains(property.Name) == true
                && currentValue is not null
                && propertyJson.Value.ValueKind == JsonValueKind.Object
            )
            {
                Apply(currentValue, propertyJson.Value.GetRawText(), serializerOptions);
                applied.Add(property.Name);
                continue;
            }

            var value = Deserialize(propertyJson.Value, targetType, serializerOptions);

            if (
                value is null
                && property.PropertyType.IsValueType
                && Nullable.GetUnderlyingType(property.PropertyType) is null
            )
                continue;

            property.SetValue(entity, value);
            applied.Add(property.Name);
        }

        return applied;
    }

    private static Dictionary<string, object?> Create(EntityEntry entry, bool original)
    {
        var snapshot = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var property in entry.Properties)
        {
            // Shadow properties (TPH discriminator, generated columns, ...) have
            // no CLR setter and cannot be restored.
            if (property.Metadata.IsShadowProperty())
                continue;

            // Generated search vectors and similar columns are not restorable and
            // only add noise to the audit payload.
            if (property.Metadata.ValueGenerated == ValueGenerated.OnAddOrUpdate)
                continue;

            snapshot[property.Metadata.Name] = original
                ? SerializeValue(property.OriginalValue, property.Metadata.ClrType)
                : SerializeValue(property.CurrentValue, property.Metadata.ClrType);
        }

        foreach (var navigation in entry.Navigations)
        {
            if (navigation.Metadata.TargetEntityType.IsOwned())
            {
                CaptureOwned(snapshot, entry, navigation, original);
            }
            else if (navigation is CollectionEntry collection)
            {
                CaptureReferenceIds(snapshot, collection);
            }
        }

        return snapshot;
    }

    private static void CaptureOwned(
        Dictionary<string, object?> snapshot,
        EntityEntry owner,
        NavigationEntry navigation,
        bool original
    )
    {
        var ownedEntries = GetOwnedEntries(owner, navigation, original).ToList();

        if (navigation is CollectionEntry)
        {
            snapshot[navigation.Metadata.Name] = ownedEntries
                .Select(entry => Create(entry, original))
                .ToList();
        }
        else
        {
            snapshot[navigation.Metadata.Name] =
                ownedEntries.Count > 0 ? Create(ownedEntries[0], original) : null;
        }
    }

    /// <summary>
    /// Finds the owned entries that belong to <paramref name="owner"/> for a
    /// navigation. For the before snapshot we use every owned entry that existed
    /// before the save (Unchanged/Modified/Deleted); for the after snapshot we use
    /// every owned entry that exists after the save (Unchanged/Modified/Added).
    /// This also handles replacing an owned object, where EF tracks the old entry
    /// as Deleted and the new entry as Added.
    /// </summary>
    private static IEnumerable<EntityEntry> GetOwnedEntries(
        EntityEntry owner,
        NavigationEntry navigation,
        bool original
    )
    {
        var ownership = navigation.Metadata.TargetEntityType.FindOwnership();
        if (ownership is null)
            yield break;

        foreach (var candidate in owner.Context.ChangeTracker.Entries())
        {
            if (!candidate.Metadata.IsOwned())
                continue;

            if (candidate.Metadata != navigation.Metadata.TargetEntityType)
                continue;

            if (
                original
                    ? candidate.State == EntityState.Added
                    : candidate.State == EntityState.Deleted
            )
                continue;

            if (!BelongsTo(candidate, owner, ownership))
                continue;

            yield return candidate;
        }
    }

    private static bool BelongsTo(EntityEntry ownedEntry, EntityEntry owner, IForeignKey ownership)
    {
        if (
            ownership.Properties.Count == 0
            || ownership.Properties.Count != ownership.PrincipalKey.Properties.Count
        )
            return false;

        for (var index = 0; index < ownership.Properties.Count; index++)
        {
            var dependent = ownedEntry.Property(ownership.Properties[index].Name);
            var principal = owner.Property(ownership.PrincipalKey.Properties[index].Name);

            var dependentValue = dependent.CurrentValue ?? dependent.OriginalValue;
            var principalValue = principal.CurrentValue ?? principal.OriginalValue;

            if (!Equals(dependentValue, principalValue))
                return false;
        }

        return true;
    }

    private static void CaptureReferenceIds(
        Dictionary<string, object?> snapshot,
        CollectionEntry collection
    )
    {
        if (collection.CurrentValue is not IEnumerable values)
            return;

        var ids = new List<Guid>();
        foreach (var value in values)
        {
            if (value is null)
                continue;

            var idProperty = value.GetType().GetProperty("Id");
            if (idProperty?.GetValue(value) is Guid id)
                ids.Add(id);
        }

        if (ids.Count == 0)
            return;

        if (
            snapshot.TryGetValue("$refs", out var existing) is false
            || existing is not Dictionary<string, object?> refs
        )
        {
            refs = new Dictionary<string, object?>(StringComparer.Ordinal);
            snapshot["$refs"] = refs;
        }

        refs[collection.Metadata.Name] = ids;
    }

    private static object? Deserialize(
        JsonElement element,
        Type targetType,
        JsonSerializerOptions options
    )
    {
        if (element.ValueKind == JsonValueKind.Null)
            return null;

        var raw =
            element.ValueKind == JsonValueKind.Object
                ? MoveDiscriminatorsFirst(element)
                : element.GetRawText();

        return JsonSerializer.Deserialize(raw, targetType, options);
    }

    private static JsonElement? SerializeValue(object? value, Type declaredType)
    {
        if (value is null)
            return null;

        // Use the declared (model) type so polymorphic values such as Formula
        // trees get their discriminator written even though the surrounding
        // snapshot is stored in a dictionary.
        return JsonSerializer.SerializeToElement(value, declaredType, Options);
    }

    /// <summary>
    /// PostgreSQL jsonb does not preserve object key order. System.Text.Json
    /// requires a polymorphic discriminator to be the first property, so rewrite
    /// the payload before deserializing it.
    /// </summary>
    private static string MoveDiscriminatorsFirst(JsonElement element)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            WriteNormalized(element, writer);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static void WriteNormalized(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                if (element.TryGetProperty("$type", out var discriminator))
                {
                    writer.WritePropertyName("$type");
                    WriteNormalized(discriminator, writer);
                }

                foreach (var property in element.EnumerateObject())
                {
                    if (property.Name == "$type")
                        continue;

                    writer.WritePropertyName(property.Name);
                    WriteNormalized(property.Value, writer);
                }

                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                    WriteNormalized(item, writer);
                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static DateTimeOffset TruncateToMicroseconds(DateTimeOffset value) =>
        new(value.Ticks - value.Ticks % 10, value.Offset);

    private static DateTime TruncateToMicroseconds(DateTime value) =>
        new(value.Ticks - value.Ticks % 10, value.Kind);

    private sealed class MicrosecondDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        ) => reader.GetDateTimeOffset();

        public override void Write(
            Utf8JsonWriter writer,
            DateTimeOffset value,
            JsonSerializerOptions options
        ) => writer.WriteStringValue(TruncateToMicroseconds(value));
    }

    private sealed class MicrosecondDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        ) => reader.GetDateTime();

        public override void Write(
            Utf8JsonWriter writer,
            DateTime value,
            JsonSerializerOptions options
        ) => writer.WriteStringValue(TruncateToMicroseconds(value));
    }

    private static Type ResolveTargetType(Type type)
    {
        if (!type.IsInterface || !type.IsGenericType)
            return type;

        var definition = type.GetGenericTypeDefinition();
        if (
            definition == typeof(ICollection<>)
            || definition == typeof(IList<>)
            || definition == typeof(IEnumerable<>)
            || definition == typeof(IReadOnlyCollection<>)
            || definition == typeof(IReadOnlyList<>)
        )
        {
            return typeof(List<>).MakeGenericType(type.GetGenericArguments()[0]);
        }

        return type;
    }
}
