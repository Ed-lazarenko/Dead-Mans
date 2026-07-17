using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGameActiveTeam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ActiveTeamId",
                table: "games",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_games_ActiveTeamId",
                table: "games",
                column: "ActiveTeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_games_game_teams_ActiveTeamId",
                table: "games",
                column: "ActiveTeamId",
                principalTable: "game_teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_games_game_teams_ActiveTeamId",
                table: "games");

            migrationBuilder.DropIndex(
                name: "IX_games_ActiveTeamId",
                table: "games");

            migrationBuilder.DropColumn(
                name: "ActiveTeamId",
                table: "games");
        }
    }
}
