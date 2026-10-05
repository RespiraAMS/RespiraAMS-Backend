using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Respira.Clinical.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Clinical_migrations_v3_add_treatment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "treatments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    TreatmentSite = table.Column<int>(type: "integer", nullable: false),
                    MedicineIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    PathogenIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    CriteriaIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_treatments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AntibioticTreatment",
                columns: table => new
                {
                    MedicinesId = table.Column<Guid>(type: "uuid", nullable: false),
                    TreatmentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AntibioticTreatment", x => new { x.MedicinesId, x.TreatmentId });
                    table.ForeignKey(
                        name: "FK_AntibioticTreatment_antibiotics_MedicinesId",
                        column: x => x.MedicinesId,
                        principalTable: "antibiotics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AntibioticTreatment_treatments_TreatmentId",
                        column: x => x.TreatmentId,
                        principalTable: "treatments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "treatment_criteria",
                columns: table => new
                {
                    CriteriaId = table.Column<Guid>(type: "uuid", nullable: false),
                    TreatmentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_treatment_criteria", x => new { x.CriteriaId, x.TreatmentId });
                    table.ForeignKey(
                        name: "FK_treatment_criteria_criteria_CriteriaId",
                        column: x => x.CriteriaId,
                        principalTable: "criteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_treatment_criteria_treatments_TreatmentId",
                        column: x => x.TreatmentId,
                        principalTable: "treatments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "treatment_pathogens",
                columns: table => new
                {
                    PathogensId = table.Column<Guid>(type: "uuid", nullable: false),
                    TreatmentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_treatment_pathogens", x => new { x.PathogensId, x.TreatmentId });
                    table.ForeignKey(
                        name: "FK_treatment_pathogens_pathogens_PathogensId",
                        column: x => x.PathogensId,
                        principalTable: "pathogens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_treatment_pathogens_treatments_TreatmentId",
                        column: x => x.TreatmentId,
                        principalTable: "treatments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AntibioticTreatment_TreatmentId",
                table: "AntibioticTreatment",
                column: "TreatmentId");

            migrationBuilder.CreateIndex(
                name: "IX_treatment_criteria_TreatmentId",
                table: "treatment_criteria",
                column: "TreatmentId");

            migrationBuilder.CreateIndex(
                name: "IX_treatment_pathogens_TreatmentId",
                table: "treatment_pathogens",
                column: "TreatmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AntibioticTreatment");

            migrationBuilder.DropTable(
                name: "treatment_criteria");

            migrationBuilder.DropTable(
                name: "treatment_pathogens");

            migrationBuilder.DropTable(
                name: "treatments");
        }
    }
}
