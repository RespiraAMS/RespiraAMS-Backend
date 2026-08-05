using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitClinicalDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "antibiotic_groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_antibiotic_groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_antibiotic_groups_antibiotic_groups_ParentId",
                        column: x => x.ParentId,
                        principalTable: "antibiotic_groups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "criteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Value = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_criteria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "diseases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IcuScoreThreshold = table.Column<int>(type: "integer", nullable: false),
                    Algorithm = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_diseases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pathogens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    AntibioticIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pathogens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "antibiotics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    AntibioticGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    DosageIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    PathogenIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_antibiotics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_antibiotics_antibiotic_groups_AntibioticGroupId",
                        column: x => x.AntibioticGroupId,
                        principalTable: "antibiotic_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "icu_hospitalize_criteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiseaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriterionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_icu_hospitalize_criteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_icu_hospitalize_criteria_criteria_CriterionId",
                        column: x => x.CriterionId,
                        principalTable: "criteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_icu_hospitalize_criteria_diseases_DiseaseId",
                        column: x => x.DiseaseId,
                        principalTable: "diseases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "antibiograms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PathogenId = table.Column<Guid>(type: "uuid", nullable: false),
                    MicLevel = table.Column<string>(type: "text", nullable: false),
                    MicIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    FirstPriorityMedicineIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    SecondPriorityMedicineIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_antibiograms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_antibiograms_pathogens_PathogenId",
                        column: x => x.PathogenId,
                        principalTable: "pathogens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "disease_causes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiseaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    PathogenId = table.Column<Guid>(type: "uuid", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    TreatmentSite = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_disease_causes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_disease_causes_diseases_DiseaseId",
                        column: x => x.DiseaseId,
                        principalTable: "diseases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_disease_causes_pathogens_PathogenId",
                        column: x => x.PathogenId,
                        principalTable: "pathogens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "empiric_treatment_protocols",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Issuer = table.Column<string>(type: "text", nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    DiseaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    TreatmentSite = table.Column<string>(type: "text", nullable: false),
                    SpecialInfectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    OtherCriteriaIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    MedicineIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_empiric_treatment_protocols", x => x.Id);
                    table.ForeignKey(
                        name: "FK_empiric_treatment_protocols_diseases_DiseaseId",
                        column: x => x.DiseaseId,
                        principalTable: "diseases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_empiric_treatment_protocols_pathogens_SpecialInfectionId",
                        column: x => x.SpecialInfectionId,
                        principalTable: "pathogens",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "resistance_risk_factors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiseaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CriterionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PathogenId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resistance_risk_factors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_resistance_risk_factors_criteria_CriterionId",
                        column: x => x.CriterionId,
                        principalTable: "criteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_resistance_risk_factors_diseases_DiseaseId",
                        column: x => x.DiseaseId,
                        principalTable: "diseases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_resistance_risk_factors_pathogens_PathogenId",
                        column: x => x.PathogenId,
                        principalTable: "pathogens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "antibiotic_pathogen",
                columns: table => new
                {
                    AntibioticSpectraId = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicinesId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_antibiotic_pathogen", x => new { x.AntibioticSpectraId, x.MedicinesId });
                    table.ForeignKey(
                        name: "FK_antibiotic_pathogen_antibiotics_MedicinesId",
                        column: x => x.MedicinesId,
                        principalTable: "antibiotics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_antibiotic_pathogen_pathogens_AntibioticSpectraId",
                        column: x => x.AntibioticSpectraId,
                        principalTable: "pathogens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dosages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AntibioticId = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteOfAdministration = table.Column<string>(type: "text", nullable: false),
                    Dose = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    GlomerularFiltrationRate = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dosages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dosages_antibiotics_AntibioticId",
                        column: x => x.AntibioticId,
                        principalTable: "antibiotics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "antibiogram_first_priority_medicines",
                columns: table => new
                {
                    Antibiogram1Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstPriorityMedicinesId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_antibiogram_first_priority_medicines", x => new { x.Antibiogram1Id, x.FirstPriorityMedicinesId });
                    table.ForeignKey(
                        name: "FK_antibiogram_first_priority_medicines_antibiograms_Antibiogr~",
                        column: x => x.Antibiogram1Id,
                        principalTable: "antibiograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_antibiogram_first_priority_medicines_antibiotics_FirstPrior~",
                        column: x => x.FirstPriorityMedicinesId,
                        principalTable: "antibiotics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "antibiogram_mic_groups",
                columns: table => new
                {
                    AntibiogramId = table.Column<Guid>(type: "uuid", nullable: false),
                    MicsId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_antibiogram_mic_groups", x => new { x.AntibiogramId, x.MicsId });
                    table.ForeignKey(
                        name: "FK_antibiogram_mic_groups_antibiograms_AntibiogramId",
                        column: x => x.AntibiogramId,
                        principalTable: "antibiograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_antibiogram_mic_groups_antibiotics_MicsId",
                        column: x => x.MicsId,
                        principalTable: "antibiotics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "antibiogram_second_priority_medicines",
                columns: table => new
                {
                    Antibiogram2Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SecondPriorityMedicinesId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_antibiogram_second_priority_medicines", x => new { x.Antibiogram2Id, x.SecondPriorityMedicinesId });
                    table.ForeignKey(
                        name: "FK_antibiogram_second_priority_medicines_antibiograms_Antibiog~",
                        column: x => x.Antibiogram2Id,
                        principalTable: "antibiograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_antibiogram_second_priority_medicines_antibiotics_SecondPri~",
                        column: x => x.SecondPriorityMedicinesId,
                        principalTable: "antibiotics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "empiric_treatment_protocol_medicine",
                columns: table => new
                {
                    EmpiricTreatmentProtocolId = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicinesId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_empiric_treatment_protocol_medicine", x => new { x.EmpiricTreatmentProtocolId, x.MedicinesId });
                    table.ForeignKey(
                        name: "FK_empiric_treatment_protocol_medicine_antibiotics_MedicinesId",
                        column: x => x.MedicinesId,
                        principalTable: "antibiotics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_empiric_treatment_protocol_medicine_empiric_treatment_proto~",
                        column: x => x.EmpiricTreatmentProtocolId,
                        principalTable: "empiric_treatment_protocols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "empiric_treatment_protocol_other_criteria",
                columns: table => new
                {
                    EmpiricTreatmentProtocolId = table.Column<Guid>(type: "uuid", nullable: false),
                    OtherCriteriaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_empiric_treatment_protocol_other_criteria", x => new { x.EmpiricTreatmentProtocolId, x.OtherCriteriaId });
                    table.ForeignKey(
                        name: "FK_empiric_treatment_protocol_other_criteria_criteria_OtherCri~",
                        column: x => x.OtherCriteriaId,
                        principalTable: "criteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_empiric_treatment_protocol_other_criteria_empiric_treatment~",
                        column: x => x.EmpiricTreatmentProtocolId,
                        principalTable: "empiric_treatment_protocols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_antibiogram_first_priority_medicines_FirstPriorityMedicines~",
                table: "antibiogram_first_priority_medicines",
                column: "FirstPriorityMedicinesId");

            migrationBuilder.CreateIndex(
                name: "IX_antibiogram_mic_groups_MicsId",
                table: "antibiogram_mic_groups",
                column: "MicsId");

            migrationBuilder.CreateIndex(
                name: "IX_antibiogram_second_priority_medicines_SecondPriorityMedicin~",
                table: "antibiogram_second_priority_medicines",
                column: "SecondPriorityMedicinesId");

            migrationBuilder.CreateIndex(
                name: "IX_antibiograms_PathogenId",
                table: "antibiograms",
                column: "PathogenId");

            migrationBuilder.CreateIndex(
                name: "IX_antibiotic_groups_Name",
                table: "antibiotic_groups",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_antibiotic_groups_ParentId",
                table: "antibiotic_groups",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_antibiotic_pathogen_MedicinesId",
                table: "antibiotic_pathogen",
                column: "MedicinesId");

            migrationBuilder.CreateIndex(
                name: "IX_antibiotics_AntibioticGroupId",
                table: "antibiotics",
                column: "AntibioticGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_antibiotics_Name_Category",
                table: "antibiotics",
                columns: new[] { "Name", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_disease_causes_DiseaseId",
                table: "disease_causes",
                column: "DiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_disease_causes_PathogenId",
                table: "disease_causes",
                column: "PathogenId");

            migrationBuilder.CreateIndex(
                name: "IX_diseases_Name",
                table: "diseases",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_dosages_AntibioticId",
                table: "dosages",
                column: "AntibioticId");

            migrationBuilder.CreateIndex(
                name: "IX_empiric_treatment_protocol_medicine_MedicinesId",
                table: "empiric_treatment_protocol_medicine",
                column: "MedicinesId");

            migrationBuilder.CreateIndex(
                name: "IX_empiric_treatment_protocol_other_criteria_OtherCriteriaId",
                table: "empiric_treatment_protocol_other_criteria",
                column: "OtherCriteriaId");

            migrationBuilder.CreateIndex(
                name: "IX_empiric_treatment_protocols_DiseaseId",
                table: "empiric_treatment_protocols",
                column: "DiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_empiric_treatment_protocols_SpecialInfectionId",
                table: "empiric_treatment_protocols",
                column: "SpecialInfectionId");

            migrationBuilder.CreateIndex(
                name: "IX_icu_hospitalize_criteria_CriterionId",
                table: "icu_hospitalize_criteria",
                column: "CriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_icu_hospitalize_criteria_DiseaseId",
                table: "icu_hospitalize_criteria",
                column: "DiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_resistance_risk_factors_CriterionId",
                table: "resistance_risk_factors",
                column: "CriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_resistance_risk_factors_DiseaseId",
                table: "resistance_risk_factors",
                column: "DiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_resistance_risk_factors_PathogenId",
                table: "resistance_risk_factors",
                column: "PathogenId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "antibiogram_first_priority_medicines");

            migrationBuilder.DropTable(
                name: "antibiogram_mic_groups");

            migrationBuilder.DropTable(
                name: "antibiogram_second_priority_medicines");

            migrationBuilder.DropTable(
                name: "antibiotic_pathogen");

            migrationBuilder.DropTable(
                name: "disease_causes");

            migrationBuilder.DropTable(
                name: "dosages");

            migrationBuilder.DropTable(
                name: "empiric_treatment_protocol_medicine");

            migrationBuilder.DropTable(
                name: "empiric_treatment_protocol_other_criteria");

            migrationBuilder.DropTable(
                name: "icu_hospitalize_criteria");

            migrationBuilder.DropTable(
                name: "resistance_risk_factors");

            migrationBuilder.DropTable(
                name: "antibiograms");

            migrationBuilder.DropTable(
                name: "antibiotics");

            migrationBuilder.DropTable(
                name: "empiric_treatment_protocols");

            migrationBuilder.DropTable(
                name: "criteria");

            migrationBuilder.DropTable(
                name: "antibiotic_groups");

            migrationBuilder.DropTable(
                name: "diseases");

            migrationBuilder.DropTable(
                name: "pathogens");
        }
    }
}
