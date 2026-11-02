using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using Respira.Saga.Infrastructure.Persistence;

namespace Respira.Saga.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SagaAuditDbContext))]
[Migration("20261030180000_InitialSagaAudit")]
public sealed class InitialSagaAudit : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AuditLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ActorAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                ActorRole = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                EntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                DetailsJson = table.Column<string>(type: "jsonb", nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_AuditLogs", x => x.Id)
        );

        migrationBuilder.CreateIndex("IX_AuditLogs_ActorAccountId", "AuditLogs", "ActorAccountId");
        migrationBuilder.CreateIndex("IX_AuditLogs_OccurredAt", "AuditLogs", "OccurredAt");
        migrationBuilder.CreateIndex(
            "IX_AuditLogs_EntityType_EntityId",
            "AuditLogs",
            new[] { "EntityType", "EntityId" }
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("AuditLogs");
}
