using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Respira.Clinical.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Clinical_migrations_v16_add_antibiogram : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "antibiograms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MicCategory = table.Column<string>(type: "text", nullable: false),
                    PathogenId = table.Column<Guid>(type: "uuid", nullable: false),
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
                name: "antibiogram_antibiotics",
                columns: table => new
                {
                    AntibiogramId = table.Column<Guid>(type: "uuid", nullable: false),
                    AntibioticsId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_antibiogram_antibiotics", x => new { x.AntibiogramId, x.AntibioticsId });
                    table.ForeignKey(
                        name: "FK_antibiogram_antibiotics_antibiograms_AntibiogramId",
                        column: x => x.AntibiogramId,
                        principalTable: "antibiograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_antibiogram_antibiotics_antibiotics_AntibioticsId",
                        column: x => x.AntibioticsId,
                        principalTable: "antibiotics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_antibiogram_antibiotics_AntibioticsId",
                table: "antibiogram_antibiotics",
                column: "AntibioticsId");

            migrationBuilder.CreateIndex(
                name: "IX_antibiograms_PathogenId",
                table: "antibiograms",
                column: "PathogenId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "antibiogram_antibiotics");

            migrationBuilder.DropTable(
                name: "antibiograms");
        }
    }
}
