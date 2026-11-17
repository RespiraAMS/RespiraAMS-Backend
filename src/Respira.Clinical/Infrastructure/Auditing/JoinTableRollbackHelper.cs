using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Auditing;

namespace Respira.Clinical.Infrastructure.Auditing;

/// <summary>
/// Executes raw SQL checks and mutations for audited many-to-many join-table entries.
/// </summary>
internal static class JoinTableRollbackHelper
{
    internal const string JoinPrefix = "__join__:";

    public static bool IsJoinEntry(AuditEntry entry) =>
        entry.EntityType.StartsWith(JoinPrefix, StringComparison.Ordinal);

    public static async Task<bool> ExistsAsync(
        ClinicalDbContext db,
        AuditEntry entry,
        CancellationToken cancellationToken
    )
    {
        var json = entry.Operation == "Deleted" ? entry.BeforeJson : entry.AfterJson;
        if (string.IsNullOrWhiteSpace(json))
            return false;

        var table = ParseJoinTable(entry.EntityType);
        var (columns, parameters) = BuildJoinParameters(db, entry.EntityType, json);
        var where = string.Join(
            " AND ",
            columns.Select((column, index) => $"{Quote(column)} = @p{index}")
        );

        var sql = $"SELECT COUNT(1)::int AS \"Value\" FROM {table} WHERE {where}";
        var count = await db
            .Database.SqlQueryRaw<int>(sql, parameters)
            .SingleAsync(cancellationToken);

        return count > 0;
    }

    public static async Task DeleteRowAsync(
        ClinicalDbContext db,
        AuditEntry entry,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(entry.AfterJson))
            return;

        var table = ParseJoinTable(entry.EntityType);
        var (columns, parameters) = BuildJoinParameters(db, entry.EntityType, entry.AfterJson);
        var where = string.Join(
            " AND ",
            columns.Select((column, index) => $"{Quote(column)} = @p{index}")
        );

        var sql = $"DELETE FROM {table} WHERE {where}";
        await db.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
    }

    public static async Task InsertRowAsync(
        ClinicalDbContext db,
        AuditEntry entry,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(entry.BeforeJson))
            return;

        var table = ParseJoinTable(entry.EntityType);
        var (columns, parameters) = BuildJoinParameters(db, entry.EntityType, entry.BeforeJson);
        var columnList = string.Join(", ", columns.Select(Quote));
        var parameterList = string.Join(", ", columns.Select((_, index) => $"@p{index}"));

        var sql = $"INSERT INTO {table} ({columnList}) VALUES ({parameterList}) ON CONFLICT DO NOTHING";
        await db.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
    }

    private static (IReadOnlyList<string> Columns, NpgsqlParameter[] Parameters) BuildJoinParameters(
        ClinicalDbContext db,
        string entityType,
        string json
    )
    {
        var rawTable = entityType[JoinPrefix.Length..];
        var metadata = db.Model
            .GetEntityTypes()
            .FirstOrDefault(type => JoinTableName(type) == rawTable);

        using var document = JsonDocument.Parse(json);
        var columns = new List<string>();
        var parameters = new List<NpgsqlParameter>();

        var index = 0;
        foreach (var property in document.RootElement.EnumerateObject())
        {
            var entityProperty = metadata
                ?.GetProperties()
                .FirstOrDefault(item =>
                    string.Equals(item.GetColumnName(), property.Name, StringComparison.OrdinalIgnoreCase)
                );

            var value = ConvertJsonValue(property.Value, entityProperty?.ClrType ?? typeof(string));
            columns.Add(property.Name);
            parameters.Add(new NpgsqlParameter($"p{index}", value ?? DBNull.Value));
            index++;
        }

        return (columns, parameters.ToArray());
    }

    private static object? ConvertJsonValue(JsonElement element, Type clrType)
    {
        if (element.ValueKind == JsonValueKind.Null)
            return DBNull.Value;

        var targetType = Nullable.GetUnderlyingType(clrType) ?? clrType;
        if (targetType == typeof(Guid))
            return Guid.Parse(element.GetString()!);
        if (targetType == typeof(string))
            return element.GetString();
        if (targetType == typeof(bool))
            return element.GetBoolean();
        if (targetType == typeof(int))
            return element.GetInt32();
        if (targetType == typeof(long))
            return element.GetInt64();
        if (targetType == typeof(decimal))
            return element.GetDecimal();
        if (targetType == typeof(double))
            return element.GetDouble();
        if (targetType == typeof(float))
            return element.GetSingle();
        if (targetType == typeof(DateTimeOffset))
            return element.GetDateTimeOffset();
        if (targetType == typeof(DateTime))
            return element.GetDateTime();
        if (targetType == typeof(DateOnly))
            return DateOnly.Parse(element.GetString()!);
        if (targetType.IsEnum)
            return Enum.Parse(targetType, element.GetString()!, ignoreCase: true);

        return JsonSerializer.Deserialize(
            element.GetRawText(),
            targetType,
            AuditSnapshot.Options
        );
    }

    private static string JoinTableName(IEntityType entityType)
    {
        var table = entityType.GetTableName();
        if (string.IsNullOrWhiteSpace(table))
            return string.Empty;

        var schema = entityType.GetSchema();
        return string.IsNullOrWhiteSpace(schema) ? table : $"{schema}.{table}";
    }

    private static string ParseJoinTable(string entityType)
    {
        var raw = entityType[JoinPrefix.Length..];
        var separator = raw.LastIndexOf('.');
        return separator < 0
            ? Quote(raw)
            : $"{Quote(raw[..separator])}.{Quote(raw[(separator + 1)..])}";
    }

    private static string Quote(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";
}
