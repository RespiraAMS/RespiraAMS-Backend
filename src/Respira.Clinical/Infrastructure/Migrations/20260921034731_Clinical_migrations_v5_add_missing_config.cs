using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Respira.Clinical.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Clinical_migrations_v5_add_missing_config : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SuspectedCauses_pathogens_PathogenId",
                table: "SuspectedCauses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SuspectedCauses",
                table: "SuspectedCauses");

            migrationBuilder.RenameTable(
                name: "SuspectedCauses",
                newName: "suspected_causes");

            migrationBuilder.RenameIndex(
                name: "IX_SuspectedCauses_PathogenId",
                table: "suspected_causes",
                newName: "IX_suspected_causes_PathogenId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_suspected_causes",
                table: "suspected_causes",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_suspected_causes_pathogens_PathogenId",
                table: "suspected_causes",
                column: "PathogenId",
                principalTable: "pathogens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_suspected_causes_pathogens_PathogenId",
                table: "suspected_causes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_suspected_causes",
                table: "suspected_causes");

            migrationBuilder.RenameTable(
                name: "suspected_causes",
                newName: "SuspectedCauses");

            migrationBuilder.RenameIndex(
                name: "IX_suspected_causes_PathogenId",
                table: "SuspectedCauses",
                newName: "IX_SuspectedCauses_PathogenId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SuspectedCauses",
                table: "SuspectedCauses",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SuspectedCauses_pathogens_PathogenId",
                table: "SuspectedCauses",
                column: "PathogenId",
                principalTable: "pathogens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
