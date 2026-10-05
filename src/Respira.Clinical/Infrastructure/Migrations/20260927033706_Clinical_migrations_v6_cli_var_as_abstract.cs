using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Respira.Clinical.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Clinical_migrations_v6_cli_var_as_abstract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ValueType",
                table: "clinical_variables");

            migrationBuilder.AddColumn<string>(
                name: "AcceptedRange",
                table: "clinical_variables",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "AcceptedValues",
                table: "clinical_variables",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "value_type",
                table: "clinical_variables",
                type: "character varying(34)",
                maxLength: 34,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedRange",
                table: "clinical_variables");

            migrationBuilder.DropColumn(
                name: "AcceptedValues",
                table: "clinical_variables");

            migrationBuilder.DropColumn(
                name: "value_type",
                table: "clinical_variables");

            migrationBuilder.AddColumn<string>(
                name: "ValueType",
                table: "clinical_variables",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
