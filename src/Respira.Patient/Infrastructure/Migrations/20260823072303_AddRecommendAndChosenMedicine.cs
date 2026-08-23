using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecommendAndChosenMedicine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MedicineRecords",
                table: "treatments",
                newName: "SystemRecommendedMedicines");

            migrationBuilder.AddColumn<string>(
                name: "DoctorChosenMedicines",
                table: "treatments",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DoctorChosenMedicines",
                table: "treatments");

            migrationBuilder.RenameColumn(
                name: "SystemRecommendedMedicines",
                table: "treatments",
                newName: "MedicineRecords");
        }
    }
}
