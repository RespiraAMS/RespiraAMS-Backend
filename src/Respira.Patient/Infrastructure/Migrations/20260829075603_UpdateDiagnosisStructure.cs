using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDiagnosisStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Crcl",
                table: "treatments");

            migrationBuilder.DropColumn(
                name: "DoctorChosenMedicines",
                table: "treatments");

            migrationBuilder.DropColumn(
                name: "Pathogen",
                table: "treatments");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "treatments");

            migrationBuilder.DropColumn(
                name: "TreatmentSite",
                table: "treatments");

            migrationBuilder.RenameColumn(
                name: "SystemRecommendedMedicines",
                table: "treatments",
                newName: "TargetedDiagnosisRecord");

            migrationBuilder.RenameColumn(
                name: "InfectionProbabilityRecords",
                table: "treatments",
                newName: "EmpiricalDiagnosisRecord");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "patients",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "patients",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "patients");

            migrationBuilder.RenameColumn(
                name: "TargetedDiagnosisRecord",
                table: "treatments",
                newName: "SystemRecommendedMedicines");

            migrationBuilder.RenameColumn(
                name: "EmpiricalDiagnosisRecord",
                table: "treatments",
                newName: "InfectionProbabilityRecords");

            migrationBuilder.AddColumn<decimal>(
                name: "Crcl",
                table: "treatments",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DoctorChosenMedicines",
                table: "treatments",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pathogen",
                table: "treatments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "treatments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TreatmentSite",
                table: "treatments",
                type: "text",
                nullable: true);
        }
    }
}
