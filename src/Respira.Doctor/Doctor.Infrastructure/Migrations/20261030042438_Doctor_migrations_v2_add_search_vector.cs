using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Respira.Doctor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Doctor_migrations_v2_add_search_vector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:unaccent", ",,");

            // Create respira_vi text search configuration
            migrationBuilder.Sql("""
                CREATE TEXT SEARCH CONFIGURATION respira_vi (COPY = simple);
                ALTER TEXT SEARCH CONFIGURATION respira_vi
                    ALTER MAPPING FOR asciiword, asciihword, hword, hword_asciipart, hword_part,
                                      hword_numpart, numword, numhword, word
                    WITH unaccent, simple;
                """);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Managers",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "respira_vi")
                .Annotation("Npgsql:TsVectorProperties", new[] { "FirstName", "LastName", "CitizenIdentificationNumber" });

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Doctors",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "respira_vi")
                .Annotation("Npgsql:TsVectorProperties", new[] { "FirstName", "LastName", "CitizenIdentificationNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Managers_SearchVector",
                table: "Managers",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_SearchVector",
                table: "Doctors",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Managers_SearchVector",
                table: "Managers");

            migrationBuilder.DropIndex(
                name: "IX_Doctors_SearchVector",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Managers");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Doctors");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:unaccent", ",,");
        }
    }
}
