using System.Text.Json;
using Respira.ServiceDefaults.Auditing;

namespace Respira.ServiceDefaults.Application.Test.Auditing;

/// <summary>
/// Tests the projection of a local audit row onto the shared Saga audit event.
/// The event carries the same identity as the local row so the central store
/// can deduplicate it by <see cref="AuditEntry.Id"/>.
/// </summary>
public sealed class AuditEventFactoryTest
{
    #region Happy path

    [Fact]
    public void Create_MapsTheAuditEntryOntoTheSharedEvent()
    {
        var entry = CreateEntry();

        var auditEvent = AuditEventFactory.Create(entry);

        Assert.Equal(entry.Id, auditEvent.EventId);
        Assert.Equal(entry.OccurredAt, auditEvent.OccurredAt);
        Assert.Equal(entry.ActorAccountId, auditEvent.ActorAccountId);
        Assert.Equal(entry.ActorRole, auditEvent.ActorRole);
        Assert.Equal(entry.Action, auditEvent.Action);
        Assert.Equal(entry.EntityType, auditEvent.EntityType);
        Assert.Equal(entry.EntityId, auditEvent.EntityId);
        Assert.Equal(entry.CorrelationId, auditEvent.CorrelationId);
    }

    [Fact]
    public void Create_EventIdIsTheEntryId_SoTheCentralStoreCanDeduplicate()
    {
        var entry = CreateEntry();

        var auditEvent = AuditEventFactory.Create(entry);

        Assert.Equal(entry.Id, auditEvent.EventId);
        Assert.NotEqual(Guid.Empty, auditEvent.EventId);
    }

    [Fact]
    public void Create_NullActorAccountId_StaysNull()
    {
        var entry = CreateEntry();
        entry.ActorAccountId = null;

        var auditEvent = AuditEventFactory.Create(entry);

        Assert.Null(auditEvent.ActorAccountId);
    }

    [Fact]
    public void Create_NullEntityId_StaysNull()
    {
        var entry = CreateEntry();
        entry.EntityId = null;

        var auditEvent = AuditEventFactory.Create(entry);

        Assert.Null(auditEvent.EntityId);
    }

    [Fact]
    public void Create_NullCorrelationId_StaysNull()
    {
        var entry = CreateEntry();
        entry.CorrelationId = null;

        var auditEvent = AuditEventFactory.Create(entry);

        Assert.Null(auditEvent.CorrelationId);
    }

    #endregion

    #region Details payload

    [Fact]
    public void Create_DetailsJsonContainsTheOperation()
    {
        var entry = CreateEntry();
        entry.Operation = "SoftDeleted";

        var auditEvent = AuditEventFactory.Create(entry);

        using var document = JsonDocument.Parse(auditEvent.DetailsJson);
        Assert.Equal("SoftDeleted", document.RootElement.GetProperty("operation").GetString());
    }

    [Fact]
    public void Create_DetailsJsonContainsTheChangeSetId()
    {
        var entry = CreateEntry();

        var auditEvent = AuditEventFactory.Create(entry);

        using var document = JsonDocument.Parse(auditEvent.DetailsJson);
        Assert.Equal(entry.ChangeSetId, document.RootElement.GetProperty("changeSetId").GetGuid());
    }

    [Fact]
    public void Create_DetailsJsonContainsTheChangedFieldsAsJsonArray()
    {
        var entry = CreateEntry();
        entry.ChangedFieldsJson = """["Name","Quantity"]""";

        var auditEvent = AuditEventFactory.Create(entry);

        using var document = JsonDocument.Parse(auditEvent.DetailsJson);
        var changedFields = document.RootElement.GetProperty("changedFields");

        Assert.Equal(JsonValueKind.Array, changedFields.ValueKind);
        Assert.Equal(2, changedFields.GetArrayLength());
        Assert.Equal("Name", changedFields[0].GetString());
        Assert.Equal("Quantity", changedFields[1].GetString());
    }

    [Fact]
    public void Create_DetailsJsonContainsTheParsedBeforeSnapshot()
    {
        var entry = CreateEntry();
        entry.BeforeJson = """{"Name":"Aspirin","Quantity":10}""";

        var auditEvent = AuditEventFactory.Create(entry);

        using var document = JsonDocument.Parse(auditEvent.DetailsJson);
        var before = document.RootElement.GetProperty("before");

        Assert.Equal(JsonValueKind.Object, before.ValueKind);
        Assert.Equal("Aspirin", before.GetProperty("Name").GetString());
        Assert.Equal(10, before.GetProperty("Quantity").GetInt32());
    }

    [Fact]
    public void Create_DetailsJsonContainsTheParsedAfterSnapshot()
    {
        var entry = CreateEntry();
        entry.AfterJson = """{"Name":"Paracetamol","Quantity":20}""";

        var auditEvent = AuditEventFactory.Create(entry);

        using var document = JsonDocument.Parse(auditEvent.DetailsJson);
        var after = document.RootElement.GetProperty("after");

        Assert.Equal(JsonValueKind.Object, after.ValueKind);
        Assert.Equal("Paracetamol", after.GetProperty("Name").GetString());
        Assert.Equal(20, after.GetProperty("Quantity").GetInt32());
    }

    [Fact]
    public void Create_DetailsJsonContainsTheRequestPath()
    {
        var entry = CreateEntry();
        entry.RequestPath = "/api/v1/antibiotics";

        var auditEvent = AuditEventFactory.Create(entry);

        using var document = JsonDocument.Parse(auditEvent.DetailsJson);
        Assert.Equal("/api/v1/antibiotics", document.RootElement.GetProperty("requestPath").GetString());
    }

    [Fact]
    public void Create_DetailsJsonContainsTheTraceId()
    {
        var entry = CreateEntry();
        entry.TraceId = "trace-42";

        var auditEvent = AuditEventFactory.Create(entry);

        using var document = JsonDocument.Parse(auditEvent.DetailsJson);
        Assert.Equal("trace-42", document.RootElement.GetProperty("traceId").GetString());
    }

    #endregion

    #region Empty snapshots

    [Fact]
    public void Create_BlankBeforeJson_ProducesANullBeforeKey()
    {
        var entry = CreateEntry();
        entry.BeforeJson = null;

        var auditEvent = AuditEventFactory.Create(entry);

        using var document = JsonDocument.Parse(auditEvent.DetailsJson);
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("before").ValueKind);
    }

    [Fact]
    public void Create_WhitespaceBeforeJson_ProducesANullBeforeKey()
    {
        var entry = CreateEntry();
        entry.BeforeJson = "   ";

        var auditEvent = AuditEventFactory.Create(entry);

        using var document = JsonDocument.Parse(auditEvent.DetailsJson);
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("before").ValueKind);
    }

    [Fact]
    public void Create_BlankAfterJson_ProducesANullAfterKey()
    {
        var entry = CreateEntry();
        entry.AfterJson = null;

        var auditEvent = AuditEventFactory.Create(entry);

        using var document = JsonDocument.Parse(auditEvent.DetailsJson);
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("after").ValueKind);
    }

    [Fact]
    public void Create_BlankChangedFieldsJson_ProducesANullChangedFieldsKey()
    {
        var entry = CreateEntry();
        entry.ChangedFieldsJson = "  ";

        var auditEvent = AuditEventFactory.Create(entry);

        using var document = JsonDocument.Parse(auditEvent.DetailsJson);
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("changedFields").ValueKind);
    }

    #endregion

    #region Edge cases

    [Fact]
    public void Create_DetailsJsonIsValidJsonThatRoundTrips()
    {
        var entry = CreateEntry();

        var auditEvent = AuditEventFactory.Create(entry);

        using var document = JsonDocument.Parse(auditEvent.DetailsJson);
        Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);

        var roundTripped = JsonSerializer.Serialize(document.RootElement, AuditSnapshot.Options);
        using var reparsed = JsonDocument.Parse(roundTripped);

        Assert.Equal(entry.Operation, reparsed.RootElement.GetProperty("operation").GetString());
        Assert.Equal(entry.ChangeSetId, reparsed.RootElement.GetProperty("changeSetId").GetGuid());
        Assert.Equal(entry.RequestPath, reparsed.RootElement.GetProperty("requestPath").GetString());
    }

    [Fact]
    public void Create_NullEntry_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => AuditEventFactory.Create(null!));
    }

    #endregion

    private static AuditEntry CreateEntry() =>
        new()
        {
            Id = Guid.CreateVersion7(),
            ChangeSetId = Guid.CreateVersion7(),
            OccurredAt = new DateTimeOffset(2024, 5, 6, 7, 8, 9, TimeSpan.Zero),
            ActorAccountId = Guid.CreateVersion7(),
            ActorRole = "Nurse",
            Action = "Clinical.Antibiotic.Created",
            EntityType = "Antibiotic",
            EntityId = Guid.CreateVersion7(),
            CorrelationId = Guid.CreateVersion7(),
            Operation = "Created",
            BeforeJson = """{"Name":"Aspirin","Quantity":10}""",
            AfterJson = """{"Name":"Aspirin","Quantity":10}""",
            ChangedFieldsJson = """["Name","Quantity"]""",
            RequestPath = "/api/v1/antibiotics",
            TraceId = "trace-1",
        };
}
