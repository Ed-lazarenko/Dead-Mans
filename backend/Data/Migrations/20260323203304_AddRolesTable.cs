using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRolesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    Id = table.Column<short>(type: "smallint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "Id", "Code", "CreatedAtUtc", "Description", "Name", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { (short)1, "viewer", new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc), "Viewer role with basic registration capabilities.", "Viewer", new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { (short)2, "moderator", new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc), "Moderator role that helps manage game operations.", "Moderator", new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { (short)3, "admin", new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc), "Administrator role with full management access.", "Administrator", new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_roles_Code",
                table: "roles",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "roles");
        }
    }
}
