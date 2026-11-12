using Respira.Saga.Domain.Entities;

namespace Respira.Saga.Domain.Test.Entities
{
    /// <summary>
    /// The audit entry is a plain data carrier: the handler that stores it assigns the id and
    /// the timestamp, so the entity itself must never invent values. These tests pin that
    /// contract down so a future "helpful" default cannot silently change what gets written
    /// to the audit table.
    /// </summary>
    public class AuditLogEntryTest
    {
        [Fact]
        public void NewEntry_HasAnEmptyId()
        {
            // The id is assigned by the handler (it is the audit event id), never by the entity.
            var entry = new AuditLogEntry();

            Assert.Equal(Guid.Empty, entry.Id);
        }

        [Fact]
        public void NewEntry_HasAnEmptyActorRole()
        {
            var entry = new AuditLogEntry();

            Assert.Equal(string.Empty, entry.ActorRole);
        }

        [Fact]
        public void NewEntry_HasAnEmptyAction()
        {
            var entry = new AuditLogEntry();

            Assert.Equal(string.Empty, entry.Action);
        }

        [Fact]
        public void NewEntry_HasAnEmptyEntityType()
        {
            var entry = new AuditLogEntry();

            Assert.Equal(string.Empty, entry.EntityType);
        }

        [Fact]
        public void NewEntry_HasAnEmptyJsonObjectAsDetails()
        {
            var entry = new AuditLogEntry();

            Assert.Equal("{}", entry.DetailsJson);
        }

        [Fact]
        public void NewEntry_HasNoActorAccountId()
        {
            var entry = new AuditLogEntry();

            Assert.Null(entry.ActorAccountId);
        }

        [Fact]
        public void NewEntry_HasNoEntityId()
        {
            var entry = new AuditLogEntry();

            Assert.Null(entry.EntityId);
        }

        [Fact]
        public void NewEntry_HasNoCorrelationId()
        {
            var entry = new AuditLogEntry();

            Assert.Null(entry.CorrelationId);
        }

        [Fact]
        public void NewEntry_HasTheDefaultOccurrenceTimestamp()
        {
            var entry = new AuditLogEntry();

            Assert.Equal(default, entry.OccurredAt);
        }

        [Fact]
        public void EveryProperty_CanBeAssignedAndReadBack()
        {
            var id = Guid.CreateVersion7();
            var actorAccountId = Guid.CreateVersion7();
            var entityId = Guid.CreateVersion7();
            var correlationId = Guid.CreateVersion7();
            var occurredAt = DateTimeOffset.UtcNow;

            var entry = new AuditLogEntry
            {
                Id = id,
                OccurredAt = occurredAt,
                ActorAccountId = actorAccountId,
                ActorRole = "Admin",
                Action = "ProfileCreated",
                EntityType = "DoctorProfile",
                EntityId = entityId,
                CorrelationId = correlationId,
                DetailsJson = """{"role":"Doctor"}""",
            };

            Assert.Equal(id, entry.Id);
            Assert.Equal(occurredAt, entry.OccurredAt);
            Assert.Equal(actorAccountId, entry.ActorAccountId);
            Assert.Equal("Admin", entry.ActorRole);
            Assert.Equal("ProfileCreated", entry.Action);
            Assert.Equal("DoctorProfile", entry.EntityType);
            Assert.Equal(entityId, entry.EntityId);
            Assert.Equal(correlationId, entry.CorrelationId);
            Assert.Equal("""{"role":"Doctor"}""", entry.DetailsJson);
        }

        [Fact]
        public void AuditLogEntry_IsAClassWithAParameterlessConstructor()
        {
            var type = typeof(AuditLogEntry);

            Assert.True(type.IsClass);
            Assert.NotNull(type.GetConstructor(Type.EmptyTypes));

            var instance = Activator.CreateInstance<AuditLogEntry>();

            Assert.NotNull(instance);
        }

        [Fact]
        public void TwoEntriesWithTheSameId_AreStillDistinctObjects()
        {
            var id = Guid.CreateVersion7();

            var first = new AuditLogEntry { Id = id };
            var second = new AuditLogEntry { Id = id };

            Assert.NotSame(first, second);
            Assert.Equal(first.Id, second.Id);
        }
    }
}
