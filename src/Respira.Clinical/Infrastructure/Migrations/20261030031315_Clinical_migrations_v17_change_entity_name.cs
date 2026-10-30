using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Respira.Clinical.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Clinical_migrations_v17_change_entity_name : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "medicine_composition_antibiotics");

            migrationBuilder.DropTable(
                name: "treatment_criteria");

            migrationBuilder.DropTable(
                name: "treatment_pathogens");

            migrationBuilder.DropTable(
                name: "treatment_medicine_compositions");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "treatments");

            migrationBuilder.DropColumn(
                name: "TreatmentSite",
                table: "treatments");

            migrationBuilder.AddColumn<Guid>(
                name: "TreatmentProtocolId",
                table: "treatments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "treatment_antibiotics",
                columns: table => new
                {
                    CombinationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TreatmentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_treatment_antibiotics", x => new { x.CombinationId, x.TreatmentId });
                    table.ForeignKey(
                        name: "FK_treatment_antibiotics_antibiotics_CombinationId",
                        column: x => x.CombinationId,
                        principalTable: "antibiotics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_treatment_antibiotics_treatments_TreatmentId",
                        column: x => x.TreatmentId,
                        principalTable: "treatments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "treatment_protocols",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    TreatmentSite = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_treatment_protocols", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "treatment_protocol_criteria",
                columns: table => new
                {
                    CriteriaId = table.Column<Guid>(type: "uuid", nullable: false),
                    TreatmentProtocolId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_treatment_protocol_criteria", x => new { x.CriteriaId, x.TreatmentProtocolId });
                    table.ForeignKey(
                        name: "FK_treatment_protocol_criteria_criteria_CriteriaId",
                        column: x => x.CriteriaId,
                        principalTable: "criteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_treatment_protocol_criteria_treatment_protocols_TreatmentPr~",
                        column: x => x.TreatmentProtocolId,
                        principalTable: "treatment_protocols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "treatment_protocol_pathogens",
                columns: table => new
                {
                    PathogensId = table.Column<Guid>(type: "uuid", nullable: false),
                    TreatmentProtocolId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_treatment_protocol_pathogens", x => new { x.PathogensId, x.TreatmentProtocolId });
                    table.ForeignKey(
                        name: "FK_treatment_protocol_pathogens_pathogens_PathogensId",
                        column: x => x.PathogensId,
                        principalTable: "pathogens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_treatment_protocol_pathogens_treatment_protocols_TreatmentP~",
                        column: x => x.TreatmentProtocolId,
                        principalTable: "treatment_protocols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_treatments_TreatmentProtocolId",
                table: "treatments",
                column: "TreatmentProtocolId");

            migrationBuilder.CreateIndex(
                name: "IX_treatment_antibiotics_TreatmentId",
                table: "treatment_antibiotics",
                column: "TreatmentId");

            migrationBuilder.CreateIndex(
                name: "IX_treatment_protocol_criteria_TreatmentProtocolId",
                table: "treatment_protocol_criteria",
                column: "TreatmentProtocolId");

            migrationBuilder.CreateIndex(
                name: "IX_treatment_protocol_pathogens_TreatmentProtocolId",
                table: "treatment_protocol_pathogens",
                column: "TreatmentProtocolId");

            migrationBuilder.AddForeignKey(
                name: "FK_treatments_treatment_protocols_TreatmentProtocolId",
                table: "treatments",
                column: "TreatmentProtocolId",
                principalTable: "treatment_protocols",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_treatments_treatment_protocols_TreatmentProtocolId",
                table: "treatments");

            migrationBuilder.DropTable(
                name: "treatment_antibiotics");

            migrationBuilder.DropTable(
                name: "treatment_protocol_criteria");

            migrationBuilder.DropTable(
                name: "treatment_protocol_pathogens");

            migrationBuilder.DropTable(
                name: "treatment_protocols");

            migrationBuilder.DropIndex(
                name: "IX_treatments_TreatmentProtocolId",
                table: "treatments");

            migrationBuilder.DropColumn(
                name: "TreatmentProtocolId",
                table: "treatments");

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "treatments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TreatmentSite",
                table: "treatments",
                type: "text",
                nullable: false,
                defaultValue: "");

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
                name: "treatment_medicine_compositions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TreatmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                name: "IX_treatment_criteria_TreatmentId",
                table: "treatment_criteria",
                column: "TreatmentId");

            migrationBuilder.CreateIndex(
                name: "IX_treatment_medicine_compositions_TreatmentId",
                table: "treatment_medicine_compositions",
                column: "TreatmentId");

            migrationBuilder.CreateIndex(
                name: "IX_treatment_pathogens_TreatmentId",
                table: "treatment_pathogens",
                column: "TreatmentId");
        }
    }
}
