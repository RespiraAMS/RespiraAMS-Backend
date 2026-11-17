using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Respira.Media.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Media_migrations_v1_init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MediaAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: true),
                    ObjectKey = table.Column<string>(type: "text", nullable: true),
                    BucketName = table.Column<string>(type: "text", nullable: true),
                    ContentType = table.Column<string>(type: "text", nullable: true),
                    Size = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAssets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_BucketName",
                table: "MediaAssets",
                column: "BucketName");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_ContentType",
                table: "MediaAssets",
                column: "ContentType");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_FileName",
                table: "MediaAssets",
                column: "FileName");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_ObjectKey",
                table: "MediaAssets",
                column: "ObjectKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MediaAssets");
        }
    }
}
