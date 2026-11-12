using System.Text.Json;
using Moq;
using Npgsql;
using Respira.Saga.Domain.Enums;
using Respira.Saga.Infrastructure.Sagas;
using Wolverine.Persistence.Durability;
using Wolverine.RDBMS;
using Wolverine.RDBMS.Sagas;

namespace Respira.Saga.Infrastructure.Test.Sagas;

public class SagaStateReaderUnitTest
{
    [Fact]
    public void Constructor_NonDatabaseBackedMessageStore_ThrowsInvalidOperationException()
    {
        var nonDatabaseStore = Mock.Of<IMessageStore>();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new SagaStateReader(nonDatabaseStore)
        );

        Assert.Equal(
            "The saga state reader requires Wolverine's database-backed message store",
            exception.Message
        );
    }
}

public sealed class SagaStateReaderPostgresTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    public sealed class FakeSagaState
    {
        public Guid Id { get; set; }
        public CreateStep CurrentStep { get; set; }
        public string? ErrorMessage { get; set; }
    }

    private readonly string _connectionString;
    private readonly string _tableName = new SagaTableDefinition(
        typeof(FakeSagaState),
        null
    ).TableName;
    private NpgsqlDataSource _dataSource = null!;

    public SagaStateReaderPostgresTest(PostgresFixture fixture)
    {
        _connectionString = fixture.ConnectionString;
    }

    public async ValueTask InitializeAsync()
    {
        _dataSource = NpgsqlDataSource.Create(_connectionString);
        await using var conn = await _dataSource.OpenConnectionAsync(
            TestContext.Current.CancellationToken
        );
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            $"DROP TABLE IF EXISTS {_tableName}; DROP SCHEMA IF EXISTS custom_saga CASCADE;";
        await cmd.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _dataSource.DisposeAsync();
    }

    private SagaStateReader CreateReader(string schemaName = "")
    {
        var dbMock = new Mock<IMessageDatabase>();
        dbMock.SetupGet(x => x.DataSource).Returns(_dataSource);
        dbMock.SetupGet(x => x.SchemaName).Returns(schemaName);
        return new SagaStateReader(dbMock.Object);
    }

    private async Task CreateSagaTableAsync(string qualifiedTable)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(
            TestContext.Current.CancellationToken
        );
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            $"CREATE TABLE IF NOT EXISTS {qualifiedTable} (id uuid PRIMARY KEY, body jsonb NULL);";
        await cmd.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ReadAsync_WhenSagaTableDoesNotExist_ReturnsNull()
    {
        var reader = CreateReader();

        var state = await reader.ReadAsync<FakeSagaState>(
            Guid.CreateVersion7(),
            TestContext.Current.CancellationToken
        );

        Assert.Null(state);
    }

    [Fact]
    public async Task ReadAsync_WhenSagaRowDoesNotExist_ReturnsNull()
    {
        await CreateSagaTableAsync(_tableName);
        var reader = CreateReader();

        var state = await reader.ReadAsync<FakeSagaState>(
            Guid.CreateVersion7(),
            TestContext.Current.CancellationToken
        );

        Assert.Null(state);
    }

    [Fact]
    public async Task ReadAsync_WhenBodyColumnIsNull_ReturnsNull()
    {
        await CreateSagaTableAsync(_tableName);
        var sagaId = Guid.CreateVersion7();

        await using (
            var conn = await _dataSource.OpenConnectionAsync(TestContext.Current.CancellationToken)
        )
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"INSERT INTO {_tableName} (id, body) VALUES (@id, NULL)";
            cmd.Parameters.AddWithValue("id", sagaId);
            await cmd.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        var reader = CreateReader();
        var state = await reader.ReadAsync<FakeSagaState>(
            sagaId,
            TestContext.Current.CancellationToken
        );

        Assert.Null(state);
    }

    [Fact]
    public async Task ReadAsync_WhenSagaRowExists_DeserializesAndReturnsSagaState()
    {
        await CreateSagaTableAsync(_tableName);
        var expected = new FakeSagaState
        {
            Id = Guid.CreateVersion7(),
            CurrentStep = CreateStep.CreatingMedia,
            ErrorMessage = null,
        };

        await using (
            var conn = await _dataSource.OpenConnectionAsync(TestContext.Current.CancellationToken)
        )
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"INSERT INTO {_tableName} (id, body) VALUES (@id, @body::jsonb)";
            cmd.Parameters.AddWithValue("id", expected.Id);
            cmd.Parameters.AddWithValue("body", JsonSerializer.Serialize(expected));
            await cmd.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        var reader = CreateReader();
        var actual = await reader.ReadAsync<FakeSagaState>(
            expected.Id,
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(actual);
        Assert.Equal(expected.Id, actual!.Id);
        Assert.Equal(CreateStep.CreatingMedia, actual.CurrentStep);
        Assert.Null(actual.ErrorMessage);
    }

    [Fact]
    public async Task ReadAsync_WithConfiguredSchemaName_ReadsFromSchemaQualifiedTable()
    {
        await using (
            var conn = await _dataSource.OpenConnectionAsync(TestContext.Current.CancellationToken)
        )
        {
            await using var schemaCmd = conn.CreateCommand();
            schemaCmd.CommandText = "CREATE SCHEMA IF NOT EXISTS custom_saga;";
            await schemaCmd.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        var qualifiedTable = $"custom_saga.{_tableName}";
        await CreateSagaTableAsync(qualifiedTable);

        var expected = new FakeSagaState
        {
            Id = Guid.CreateVersion7(),
            CurrentStep = CreateStep.Compensating,
            ErrorMessage = "Upload failed",
        };

        await using (
            var conn = await _dataSource.OpenConnectionAsync(TestContext.Current.CancellationToken)
        )
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"INSERT INTO {qualifiedTable} (id, body) VALUES (@id, @body::jsonb)";
            cmd.Parameters.AddWithValue("id", expected.Id);
            cmd.Parameters.AddWithValue("body", JsonSerializer.Serialize(expected));
            await cmd.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        var reader = CreateReader(schemaName: "custom_saga");
        var actual = await reader.ReadAsync<FakeSagaState>(
            expected.Id,
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(actual);
        Assert.Equal(expected.Id, actual!.Id);
        Assert.Equal(CreateStep.Compensating, actual.CurrentStep);
        Assert.Equal("Upload failed", actual.ErrorMessage);
    }
}
