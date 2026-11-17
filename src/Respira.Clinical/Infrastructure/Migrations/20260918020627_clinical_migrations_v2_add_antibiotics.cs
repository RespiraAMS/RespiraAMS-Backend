using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Respira.Clinical.Infrastructure.Migrations;

/// <inheritdoc />
public partial class Clinical_migrations_v2_add_antibiotics : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "AntibioticId",
            table: "pathogens",
            type: "uuid",
            nullable: true);

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
            name: "antibiotics",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                AntibioticGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                Classification = table.Column<string>(type: "text", nullable: false),
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
                Crcl = table.Column<string>(type: "jsonb", nullable: true)
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

        migrationBuilder.CreateIndex(
            name: "IX_pathogens_AntibioticId",
            table: "pathogens",
            column: "AntibioticId");

        migrationBuilder.CreateIndex(
            name: "IX_antibiotic_groups_Name",
            table: "antibiotic_groups",
            column: "Name");

        migrationBuilder.CreateIndex(
            name: "IX_antibiotic_groups_ParentId",
            table: "antibiotic_groups",
            column: "ParentId");

        migrationBuilder.CreateIndex(
            name: "IX_antibiotics_AntibioticGroupId",
            table: "antibiotics",
            column: "AntibioticGroupId");

        migrationBuilder.CreateIndex(
            name: "IX_antibiotics_Name_Classification",
            table: "antibiotics",
            columns: ["Name", "Classification"]);

        migrationBuilder.CreateIndex(
            name: "IX_dosages_AntibioticId",
            table: "dosages",
            column: "AntibioticId");

        migrationBuilder.AddForeignKey(
            name: "FK_pathogens_antibiotics_AntibioticId",
            table: "pathogens",
            column: "AntibioticId",
            principalTable: "antibiotics",
            principalColumn: "Id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_pathogens_antibiotics_AntibioticId",
            table: "pathogens");

        migrationBuilder.DropTable(
            name: "dosages");

        migrationBuilder.DropTable(
            name: "antibiotics");

        migrationBuilder.DropTable(
            name: "antibiotic_groups");

        migrationBuilder.DropIndex(
            name: "IX_pathogens_AntibioticId",
            table: "pathogens");

        migrationBuilder.DropColumn(
            name: "AntibioticId",
            table: "pathogens");
    }
}
