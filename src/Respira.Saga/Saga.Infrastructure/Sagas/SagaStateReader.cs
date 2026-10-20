using System.Data.Common;
using System.Text.Json;
using Respira.Saga.Application.Constracts.Sagas;
using Wolverine.Persistence.Durability;
using Wolverine.RDBMS;
using Wolverine.RDBMS.Sagas;

namespace Respira.Saga.Infrastructure.Sagas
{
    /// <summary>
    /// Reads in-flight saga state from Wolverine's lightweight saga tables on PostgreSQL.
    /// Wolverine's own diagnostics reader (<c>DatabaseSagaStoreDiagnostics</c>, still broken
    /// in 6.46) binds the saga identity with <c>identity.ToString()</c>, a text parameter,
    /// while the <c>id</c> column is <c>uuid</c>, so every read fails with
    /// <c>operator does not exist: uuid = text</c>. This reader runs the same SELECT with a
    /// Guid-typed parameter. Drop it once Wolverine binds the identity by its id member type.
    /// </summary>
    public sealed class SagaStateReader(IMessageStore messageStore) : ISagaStateReader
    {
        // Wolverine registers the store as IMessageStore and casts it to the database-facing
        // interface itself (see its own DatabaseSagaStoreDiagnostics factory).
        private readonly IMessageDatabase _database =
            messageStore as IMessageDatabase
            ?? throw new InvalidOperationException(
                "The saga state reader requires Wolverine's database-backed message store"
            );

        public async Task<T?> ReadAsync<T>(Guid sagaId, CancellationToken cancellationToken = default)
            where T : class
        {
            // Matches SagaTableDefinition's default naming: the lowercased saga type name plus
            // "_saga". Custom table names through options.AddSagaType(...) are not in use.
            var tableName = new SagaTableDefinition(typeof(T), null).TableName;
            var qualifiedName = string.IsNullOrEmpty(_database.SchemaName)
                ? tableName
                : _database.TableNameFor(tableName);

            await using var connection = _database.DataSource.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = $"select body from {qualifiedName} where id = @id";

            var id = command.CreateParameter();
            id.ParameterName = "id";
            id.Value = sagaId;
            command.Parameters.Add(id);

            try
            {
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    // The saga has completed (Wolverine deletes the row) or never existed.
                    return null;
                }

                if (await reader.IsDBNullAsync(0, cancellationToken))
                {
                    return null;
                }

                // The body column is jsonb holding the bare saga object, written with the
                // default System.Text.Json options, so it is read back the same way.
                var body = reader.GetString(0);
                return string.IsNullOrEmpty(body) ? null : JsonSerializer.Deserialize<T>(body);
            }
            catch (DbException exception) when (exception.SqlState == "42P01")
            {
                // The saga table does not exist yet: no saga has ever run on this database.
                return null;
            }
        }
    }
}
