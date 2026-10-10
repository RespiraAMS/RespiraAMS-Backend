using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Respira.Clinical.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Clinical_migrations_v11_fix_typo_entity_name : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_metrics_rules_clinical_metrics_ClinicalMetricsId",
                table: "metrics_rules");

            migrationBuilder.DropForeignKey(
                name: "FK_metrics_rules_criteria_CriterionId",
                table: "metrics_rules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_metrics_rules",
                table: "metrics_rules");

            migrationBuilder.RenameTable(
                name: "metrics_rules",
                newName: "metric_rules");

            migrationBuilder.RenameColumn(
                name: "ClinicalMetricsId",
                table: "metric_rules",
                newName: "ClinicalMetricId");

            migrationBuilder.RenameIndex(
                name: "IX_metrics_rules_CriterionId",
                table: "metric_rules",
                newName: "IX_metric_rules_CriterionId");

            migrationBuilder.RenameIndex(
                name: "IX_metrics_rules_ClinicalMetricsId",
                table: "metric_rules",
                newName: "IX_metric_rules_ClinicalMetricId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_metric_rules",
                table: "metric_rules",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_metric_rules_clinical_metrics_ClinicalMetricId",
                table: "metric_rules",
                column: "ClinicalMetricId",
                principalTable: "clinical_metrics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_metric_rules_criteria_CriterionId",
                table: "metric_rules",
                column: "CriterionId",
                principalTable: "criteria",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_metric_rules_clinical_metrics_ClinicalMetricId",
                table: "metric_rules");

            migrationBuilder.DropForeignKey(
                name: "FK_metric_rules_criteria_CriterionId",
                table: "metric_rules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_metric_rules",
                table: "metric_rules");

            migrationBuilder.RenameTable(
                name: "metric_rules",
                newName: "metrics_rules");

            migrationBuilder.RenameColumn(
                name: "ClinicalMetricId",
                table: "metrics_rules",
                newName: "ClinicalMetricsId");

            migrationBuilder.RenameIndex(
                name: "IX_metric_rules_CriterionId",
                table: "metrics_rules",
                newName: "IX_metrics_rules_CriterionId");

            migrationBuilder.RenameIndex(
                name: "IX_metric_rules_ClinicalMetricId",
                table: "metrics_rules",
                newName: "IX_metrics_rules_ClinicalMetricsId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_metrics_rules",
                table: "metrics_rules",
                column: "Id");

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
    }
}
