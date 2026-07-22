using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGameActiveModifierArchiveState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAtUtc",
                table: "game_active_modifiers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_game_active_modifiers_GameId_ArchivedAtUtc",
                table: "game_active_modifiers",
                columns: new[] { "GameId", "ArchivedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_game_active_modifiers_GameId_ArchivedAtUtc",
                table: "game_active_modifiers");

            migrationBuilder.DropColumn(
                name: "ArchivedAtUtc",
                table: "game_active_modifiers");
        }
    }
}
