using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Respira.Doctor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Doctor_migrations_v3_replace_manager_with_head_department_doctor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Doctors_Managers_ManagerId",
                table: "Doctors");

            // The old manager profile table and the new head of department doctor
            // profile have the same shape. Rename it instead of dropping the data.
            migrationBuilder.RenameTable(name: "Managers", newName: "HeadDepartmentDoctors");

            migrationBuilder.RenameColumn(
                name: "ManagerId",
                table: "Doctors",
                newName: "HeadDepartmentDoctorId");

            migrationBuilder.RenameIndex(
                name: "IX_Doctors_ManagerId",
                table: "Doctors",
                newName: "IX_Doctors_HeadDepartmentDoctorId");

            migrationBuilder.RenameIndex(
                name: "IX_Managers_AccountId",
                table: "HeadDepartmentDoctors",
                newName: "IX_HeadDepartmentDoctors_AccountId");

            migrationBuilder.RenameIndex(
                name: "IX_Managers_CitizenIdentificationNumber",
                table: "HeadDepartmentDoctors",
                newName: "IX_HeadDepartmentDoctors_CitizenIdentificationNumber");

            migrationBuilder.RenameIndex(
                name: "IX_Managers_MediaId",
                table: "HeadDepartmentDoctors",
                newName: "IX_HeadDepartmentDoctors_MediaId");

            migrationBuilder.RenameIndex(
                name: "IX_Managers_SearchVector",
                table: "HeadDepartmentDoctors",
                newName: "IX_HeadDepartmentDoctors_SearchVector");

            // PostgreSQL does not rename the primary key constraint together with the
            // table, so keep it aligned with the EF Core naming convention.
            migrationBuilder.Sql(
                "ALTER TABLE \"HeadDepartmentDoctors\" RENAME CONSTRAINT \"PK_Managers\" TO \"PK_HeadDepartmentDoctors\";");

            migrationBuilder.AddForeignKey(
                name: "FK_Doctors_HeadDepartmentDoctors_HeadDepartmentDoctorId",
                table: "Doctors",
                column: "HeadDepartmentDoctorId",
                principalTable: "HeadDepartmentDoctors",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Doctors_HeadDepartmentDoctors_HeadDepartmentDoctorId",
                table: "Doctors");

            migrationBuilder.Sql(
                "ALTER TABLE \"HeadDepartmentDoctors\" RENAME CONSTRAINT \"PK_HeadDepartmentDoctors\" TO \"PK_Managers\";");

            migrationBuilder.RenameIndex(
                name: "IX_HeadDepartmentDoctors_SearchVector",
                table: "HeadDepartmentDoctors",
                newName: "IX_Managers_SearchVector");

            migrationBuilder.RenameIndex(
                name: "IX_HeadDepartmentDoctors_MediaId",
                table: "HeadDepartmentDoctors",
                newName: "IX_Managers_MediaId");

            migrationBuilder.RenameIndex(
                name: "IX_HeadDepartmentDoctors_CitizenIdentificationNumber",
                table: "HeadDepartmentDoctors",
                newName: "IX_Managers_CitizenIdentificationNumber");

            migrationBuilder.RenameIndex(
                name: "IX_HeadDepartmentDoctors_AccountId",
                table: "HeadDepartmentDoctors",
                newName: "IX_Managers_AccountId");

            migrationBuilder.RenameIndex(
                name: "IX_Doctors_HeadDepartmentDoctorId",
                table: "Doctors",
                newName: "IX_Doctors_ManagerId");

            migrationBuilder.RenameColumn(
                name: "HeadDepartmentDoctorId",
                table: "Doctors",
                newName: "ManagerId");

            migrationBuilder.RenameTable(name: "HeadDepartmentDoctors", newName: "Managers");

            migrationBuilder.AddForeignKey(
                name: "FK_Doctors_Managers_ManagerId",
                table: "Doctors",
                column: "ManagerId",
                principalTable: "Managers",
                principalColumn: "Id");
        }
    }
}
