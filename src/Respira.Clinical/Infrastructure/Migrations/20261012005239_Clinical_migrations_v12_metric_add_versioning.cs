using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Respira.Clinical.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Clinical_migrations_v12_metric_add_versioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsApplied",
                table: "clinical_metrics",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Publisher",
                table: "clinical_metrics",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Version",
                table: "clinical_metrics",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApplied",
                table: "clinical_metrics");

            migrationBuilder.DropColumn(
                name: "Publisher",
                table: "clinical_metrics");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "clinical_metrics");
        }
    }
}
