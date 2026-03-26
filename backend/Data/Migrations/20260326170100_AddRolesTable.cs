using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260326170100_AddRolesTable")]
    public partial class AddRolesTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    Id = table.Column<short>(type: "smallint", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO roles ("Id", "Code", "CreatedAtUtc", "Description", "Name", "UpdatedAtUtc") VALUES
                (1, 'viewer', TIMESTAMPTZ '2026-03-23T00:00:00Z', 'Viewer role with basic registration capabilities.', 'Viewer', TIMESTAMPTZ '2026-03-23T00:00:00Z'),
                (2, 'moderator', TIMESTAMPTZ '2026-03-23T00:00:00Z', 'Moderator role that helps manage game operations.', 'Moderator', TIMESTAMPTZ '2026-03-23T00:00:00Z'),
                (3, 'admin', TIMESTAMPTZ '2026-03-23T00:00:00Z', 'Administrator role with full management access.', 'Administrator', TIMESTAMPTZ '2026-03-23T00:00:00Z');
                """
            );

            migrationBuilder.CreateIndex(
                name: "IX_roles_Code",
                table: "roles",
                column: "Code",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "roles");
        }
    }
}
