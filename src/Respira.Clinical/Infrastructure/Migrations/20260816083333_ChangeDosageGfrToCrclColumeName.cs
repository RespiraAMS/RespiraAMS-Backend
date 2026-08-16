using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeDosageGfrToCrclColumeName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "GlomerularFiltrationRate",
                table: "dosages",
                newName: "Crcl");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "antibiotics",
                newName: "Classification");

            migrationBuilder.RenameIndex(
                name: "IX_antibiotics_Name_Category",
                table: "antibiotics",
                newName: "IX_antibiotics_Name_Classification");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Crcl",
                table: "dosages",
                newName: "GlomerularFiltrationRate");

            migrationBuilder.RenameColumn(
                name: "Classification",
                table: "antibiotics",
                newName: "Category");

            migrationBuilder.RenameIndex(
                name: "IX_antibiotics_Name_Classification",
                table: "antibiotics",
                newName: "IX_antibiotics_Name_Category");
        }
    }
}
