using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Respira.Analysis.Infrastructure.Data;

#nullable disable

namespace Respira.Analysis.Infrastructure.Migrations;

[DbContext(typeof(AnalysisDbContext))]
[Migration("20261108000000_InitialAnalysis")]
public sealed class InitialAnalysis : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "clinical_analyses",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DoctorId = table.Column<Guid>(type: "uuid", nullable: true),
                MedicineId = table.Column<Guid>(type: "uuid", nullable: true),
                Start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                End = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                PatientIdsJson = table.Column<string>(type: "jsonb", nullable: false),
                TreatmentPlanIdsJson = table.Column<string>(type: "jsonb", nullable: false),
                ReportJson = table.Column<string>(type: "jsonb", nullable: false),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table => table.PrimaryKey("PK_clinical_analyses", item => item.Id)
        );

        migrationBuilder.CreateIndex(
            name: "IX_clinical_analyses_CreatedAt",
            table: "clinical_analyses",
            column: "CreatedAt"
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "clinical_analyses");
}
