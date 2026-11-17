using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Respira.ServiceDefaults.Utils.JsonbSearch;

/// <summary>
/// Free text search helpers for the services that keep their payloads in PostgreSQL
/// <c>jsonb</c> columns.
/// </summary>
/// <remarks>
/// PostgreSQL has neither a <c>lower(jsonb)</c> function nor a <c>jsonb LIKE text</c>
/// operator, so a plain <c>string.Contains</c> on a JSON column fails at runtime with
/// <c>42883: function lower(jsonb) does not exist</c>. The helper below maps a CLR
/// method onto a SQL <c>CAST(... AS text)</c> so the search runs on the JSON text.
/// </remarks>
public static class JsonbSearchFunctions
{
    /// <summary>
    /// Casts a <c>jsonb</c> column to <c>text</c> so it can be searched with the
    /// regular string operators.
    /// </summary>
    public static string ToText(string value) => value;

    /// <summary>
    /// Registers <see cref="ToText"/> on the model so EF Core emits
    /// <c>CAST(column AS text)</c> instead of the invalid <c>lower(jsonb)</c>.
    /// </summary>
    /// <param name="modelBuilder">The model of the context that owns the JSON columns.</param>
    public static void RegisterToText(ModelBuilder modelBuilder)
    {
        var textMapping = new StringTypeMapping("text", System.Data.DbType.String);

        modelBuilder
            .HasDbFunction(typeof(JsonbSearchFunctions).GetMethod(nameof(ToText))!)
            .HasTranslation(sql =>
                new SqlUnaryExpression(
                    ExpressionType.Convert,
                    sql[0],
                    typeof(string),
                    textMapping
                )
            );
    }
}
