using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardCellMediaTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "media_assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Bucket = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ObjectKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    MimeType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Scope = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_assets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "board_cell_media",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CellId = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_board_cell_media", x => x.Id);
                    table.ForeignKey(
                        name: "FK_board_cell_media_board_cells_CellId",
                        column: x => x.CellId,
                        principalTable: "board_cells",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_board_cell_media_media_assets_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalTable: "media_assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_board_cell_media_CellId",
                table: "board_cell_media",
                column: "CellId");

            migrationBuilder.CreateIndex(
                name: "IX_board_cell_media_CellId_SortOrder",
                table: "board_cell_media",
                columns: new[] { "CellId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_board_cell_media_MediaAssetId",
                table: "board_cell_media",
                column: "MediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_media_assets_Bucket_ObjectKey",
                table: "media_assets",
                columns: new[] { "Bucket", "ObjectKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_media_assets_Status",
                table: "media_assets",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "board_cell_media");

            migrationBuilder.DropTable(
                name: "media_assets");
        }
    }
}
