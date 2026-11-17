using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Respira.Clinical.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Clinical_migrations_v13_add_full_text_search : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_antibiotics_Name_Classification",
                table: "antibiotics");

            migrationBuilder.DropIndex(
                name: "IX_antibiotic_groups_Name",
                table: "antibiotic_groups");

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
                table: "pathogens",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "respira_vi")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Name", "Description" });

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "clinical_variables",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "respira_vi")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Name", "Code", "Description" });

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "clinical_metrics",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "respira_vi")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Name", "Code", "Description" });

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "antibiotics",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "respira_vi")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Name" });

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "antibiotic_groups",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "respira_vi")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Name", "Description" });

            migrationBuilder.CreateIndex(
                name: "IX_pathogens_SearchVector",
                table: "pathogens",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_clinical_variables_SearchVector",
                table: "clinical_variables",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_clinical_metrics_SearchVector",
                table: "clinical_metrics",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_antibiotics_SearchVector",
                table: "antibiotics",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_antibiotic_groups_SearchVector",
                table: "antibiotic_groups",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_pathogens_SearchVector",
                table: "pathogens");

            migrationBuilder.DropIndex(
                name: "IX_clinical_variables_SearchVector",
                table: "clinical_variables");

            migrationBuilder.DropIndex(
                name: "IX_clinical_metrics_SearchVector",
                table: "clinical_metrics");

            migrationBuilder.DropIndex(
                name: "IX_antibiotics_SearchVector",
                table: "antibiotics");

            migrationBuilder.DropIndex(
                name: "IX_antibiotic_groups_SearchVector",
                table: "antibiotic_groups");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "pathogens");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "clinical_variables");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "clinical_metrics");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "antibiotics");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "antibiotic_groups");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:unaccent", ",,");

            migrationBuilder.CreateIndex(
                name: "IX_antibiotics_Name_Classification",
                table: "antibiotics",
                columns: new[] { "Name", "Classification" });

            migrationBuilder.CreateIndex(
                name: "IX_antibiotic_groups_Name",
                table: "antibiotic_groups",
                column: "Name");
        }
    }
}
