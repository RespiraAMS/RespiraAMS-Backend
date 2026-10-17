using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Respira.Patient.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Patient_migrations_v3_add_search_vector : Migration
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
                table: "patients",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "respira_vi")
                .Annotation("Npgsql:TsVectorProperties", new[] { "FirstName", "LastName", "MedicalRecordCode" });

            migrationBuilder.CreateIndex(
                name: "IX_patients_SearchVector",
                table: "patients",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_patients_SearchVector",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "patients");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:unaccent", ",,");
        }
    }
}
