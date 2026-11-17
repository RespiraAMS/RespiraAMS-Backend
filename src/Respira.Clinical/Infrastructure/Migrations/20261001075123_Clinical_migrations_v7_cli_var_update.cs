using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Respira.Clinical.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Clinical_migrations_v7_cli_var_update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "clinical_variables",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsRequired",
                table: "clinical_variables",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NormalRange",
                table: "clinical_variables",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "clinical_variables");

            migrationBuilder.DropColumn(
                name: "IsRequired",
                table: "clinical_variables");

            migrationBuilder.DropColumn(
                name: "NormalRange",
                table: "clinical_variables");
        }
    }
}
