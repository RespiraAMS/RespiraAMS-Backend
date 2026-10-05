using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Respira.Clinical.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Clinical_migrations_v4_update_treatment_medicine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pathogens_antibiotics_AntibioticId",
                table: "pathogens");

            migrationBuilder.DropTable(
                name: "AntibioticTreatment");

            migrationBuilder.DropIndex(
                name: "IX_pathogens_AntibioticId",
                table: "pathogens");

            migrationBuilder.DropColumn(
                name: "CriteriaIds",
                table: "treatments");

            migrationBuilder.DropColumn(
                name: "MedicineIds",
                table: "treatments");

            migrationBuilder.DropColumn(
                name: "PathogenIds",
                table: "treatments");

            migrationBuilder.DropColumn(
                name: "AntibioticId",
                table: "pathogens");

            migrationBuilder.DropColumn(
                name: "DosageIds",
                table: "antibiotics");

            migrationBuilder.DropColumn(
                name: "PathogenIds",
                table: "antibiotics");

            migrationBuilder.AlterColumn<string>(
                name: "TreatmentSite",
                table: "treatments",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Severity",
                table: "treatments",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateTable(
                name: "treatment_medicine_compositions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TreatmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_treatment_medicine_compositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_treatment_medicine_compositions_treatments_TreatmentId",
                        column: x => x.TreatmentId,
                        principalTable: "treatments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "medicine_composition_antibiotics",
                columns: table => new
                {
                    AntibioticsId = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicineCompositionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medicine_composition_antibiotics", x => new { x.AntibioticsId, x.MedicineCompositionId });
                    table.ForeignKey(
                        name: "FK_medicine_composition_antibiotics_antibiotics_AntibioticsId",
                        column: x => x.AntibioticsId,
                        principalTable: "antibiotics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_medicine_composition_antibiotics_treatment_medicine_composi~",
                        column: x => x.MedicineCompositionId,
                        principalTable: "treatment_medicine_compositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_medicine_composition_antibiotics_MedicineCompositionId",
                table: "medicine_composition_antibiotics",
                column: "MedicineCompositionId");

            migrationBuilder.CreateIndex(
                name: "IX_treatment_medicine_compositions_TreatmentId",
                table: "treatment_medicine_compositions",
                column: "TreatmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "medicine_composition_antibiotics");

            migrationBuilder.DropTable(
                name: "treatment_medicine_compositions");

            migrationBuilder.AlterColumn<int>(
                name: "TreatmentSite",
                table: "treatments",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "Severity",
                table: "treatments",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<List<Guid>>(
                name: "CriteriaIds",
                table: "treatments",
                type: "uuid[]",
                nullable: false);

            migrationBuilder.AddColumn<List<Guid>>(
                name: "MedicineIds",
                table: "treatments",
                type: "uuid[]",
                nullable: false);

            migrationBuilder.AddColumn<List<Guid>>(
                name: "PathogenIds",
                table: "treatments",
                type: "uuid[]",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "AntibioticId",
                table: "pathogens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<List<Guid>>(
                name: "DosageIds",
                table: "antibiotics",
                type: "uuid[]",
                nullable: false);

            migrationBuilder.AddColumn<List<Guid>>(
                name: "PathogenIds",
                table: "antibiotics",
                type: "uuid[]",
                nullable: false);

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

            migrationBuilder.CreateIndex(
                name: "IX_pathogens_AntibioticId",
                table: "pathogens",
                column: "AntibioticId");

            migrationBuilder.CreateIndex(
                name: "IX_AntibioticTreatment_TreatmentId",
                table: "AntibioticTreatment",
                column: "TreatmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_pathogens_antibiotics_AntibioticId",
                table: "pathogens",
                column: "AntibioticId",
                principalTable: "antibiotics",
                principalColumn: "Id");
        }
    }
}
