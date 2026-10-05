using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Respira.Clinical.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Clinical_migrations_v8_metrics_rules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_scoring_rules_criteria_CriterionId",
                table: "scoring_rules");

            migrationBuilder.DropForeignKey(
                name: "FK_scoring_rules_score_metrics_ScoreMetricsId",
                table: "scoring_rules");

            migrationBuilder.DropTable(
                name: "score_metrics");

            migrationBuilder.DropPrimaryKey(
                name: "PK_scoring_rules",
                table: "scoring_rules");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "risk_factors");

            migrationBuilder.RenameTable(
                name: "scoring_rules",
                newName: "metrics_rules");

            migrationBuilder.RenameColumn(
                name: "ScoreMetricsId",
                table: "metrics_rules",
                newName: "ClinicalMetricsId");

            migrationBuilder.RenameIndex(
                name: "IX_scoring_rules_ScoreMetricsId",
                table: "metrics_rules",
                newName: "IX_metrics_rules_ClinicalMetricsId");

            migrationBuilder.RenameIndex(
                name: "IX_scoring_rules_CriterionId",
                table: "metrics_rules",
                newName: "IX_metrics_rules_CriterionId");

            migrationBuilder.AlterColumn<string>(
                name: "TreatmentSite",
                table: "suspected_causes",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Severity",
                table: "suspected_causes",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "ScoreFunction",
                table: "metrics_rules",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AddColumn<bool>(
                name: "IsMajor",
                table: "metrics_rules",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rule_type",
                table: "metrics_rules",
                type: "character varying(21)",
                maxLength: 21,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_metrics_rules",
                table: "metrics_rules",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "clinical_metrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clinical_metrics", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_metrics_rules_clinical_metrics_ClinicalMetricsId",
                table: "metrics_rules",
                column: "ClinicalMetricsId",
                principalTable: "clinical_metrics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_metrics_rules_criteria_CriterionId",
                table: "metrics_rules",
                column: "CriterionId",
                principalTable: "criteria",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_metrics_rules_clinical_metrics_ClinicalMetricsId",
                table: "metrics_rules");

            migrationBuilder.DropForeignKey(
                name: "FK_metrics_rules_criteria_CriterionId",
                table: "metrics_rules");

            migrationBuilder.DropTable(
                name: "clinical_metrics");

            migrationBuilder.DropPrimaryKey(
                name: "PK_metrics_rules",
                table: "metrics_rules");

            migrationBuilder.DropColumn(
                name: "IsMajor",
                table: "metrics_rules");

            migrationBuilder.DropColumn(
                name: "rule_type",
                table: "metrics_rules");

            migrationBuilder.RenameTable(
                name: "metrics_rules",
                newName: "scoring_rules");

            migrationBuilder.RenameColumn(
                name: "ClinicalMetricsId",
                table: "scoring_rules",
                newName: "ScoreMetricsId");

            migrationBuilder.RenameIndex(
                name: "IX_metrics_rules_CriterionId",
                table: "scoring_rules",
                newName: "IX_scoring_rules_CriterionId");

            migrationBuilder.RenameIndex(
                name: "IX_metrics_rules_ClinicalMetricsId",
                table: "scoring_rules",
                newName: "IX_scoring_rules_ScoreMetricsId");

            migrationBuilder.AlterColumn<int>(
                name: "TreatmentSite",
                table: "suspected_causes",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "Severity",
                table: "suspected_causes",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "risk_factors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "ScoreFunction",
                table: "scoring_rules",
                type: "jsonb",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_scoring_rules",
                table: "scoring_rules",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "score_metrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_score_metrics", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_scoring_rules_criteria_CriterionId",
                table: "scoring_rules",
                column: "CriterionId",
                principalTable: "criteria",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_scoring_rules_score_metrics_ScoreMetricsId",
                table: "scoring_rules",
                column: "ScoreMetricsId",
                principalTable: "score_metrics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
