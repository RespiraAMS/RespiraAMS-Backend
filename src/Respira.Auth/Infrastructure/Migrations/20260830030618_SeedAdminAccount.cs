using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent seed: the bootstrap admin may already exist if a previous run
            // inserted the row but did not record this migration in __EFMigrationsHistory.
            // ON CONFLICT DO NOTHING keeps re-applying the migration safe.
            migrationBuilder.Sql(
                """
                INSERT INTO auth_doctors ("Id", "CreatedAt", "DeletedAt", "Email", "HashPassword", "IsDeleted", "IsEmailConfirmed", "Phone", "Role", "Status", "UpdatedAt")
                VALUES (
                    '11111111-1111-1111-1111-111111111111',
                    TIMESTAMPTZ '2025-01-01T00:00:00+00:00',
                    NULL,
                    'admin@respira.ams',
                    '$2a$12$RYGvxowi6VHTYi6qMXQ7ROTagbu9XS58dlqtQdSjp1AMWQ1T6dR4C',
                    FALSE,
                    TRUE,
                    '0000000000',
                    'Admin',
                    'Active',
                    TIMESTAMPTZ '2025-01-01T00:00:00+00:00'
                )
                ON CONFLICT ("Id") DO NOTHING;
                """,
                suppressTransaction: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "auth_doctors",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));
        }
    }
}
