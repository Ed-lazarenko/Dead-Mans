using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamDisbandRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<short>(
                name: "MaxPlayersPerTeam",
                table: "games",
                type: "smallint",
                nullable: false,
                defaultValue: (short)2,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldDefaultValue: (short)3);

            migrationBuilder.AddColumn<DateTime>(
                name: "DisbandRequestedAtUtc",
                table: "game_teams",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DisbandRequestedByUserId",
                table: "game_teams",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DisbandedByUserId",
                table: "game_teams",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_game_teams_DisbandedByUserId",
                table: "game_teams",
                column: "DisbandedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_game_teams_DisbandRequestedByUserId",
                table: "game_teams",
                column: "DisbandRequestedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_game_teams_users_DisbandRequestedByUserId",
                table: "game_teams",
                column: "DisbandRequestedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_game_teams_users_DisbandedByUserId",
                table: "game_teams",
                column: "DisbandedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_game_teams_users_DisbandRequestedByUserId",
                table: "game_teams");

            migrationBuilder.DropForeignKey(
                name: "FK_game_teams_users_DisbandedByUserId",
                table: "game_teams");

            migrationBuilder.DropIndex(
                name: "IX_game_teams_DisbandedByUserId",
                table: "game_teams");

            migrationBuilder.DropIndex(
                name: "IX_game_teams_DisbandRequestedByUserId",
                table: "game_teams");

            migrationBuilder.DropColumn(
                name: "DisbandRequestedAtUtc",
                table: "game_teams");

            migrationBuilder.DropColumn(
                name: "DisbandRequestedByUserId",
                table: "game_teams");

            migrationBuilder.DropColumn(
                name: "DisbandedByUserId",
                table: "game_teams");

            migrationBuilder.AlterColumn<short>(
                name: "MaxPlayersPerTeam",
                table: "games",
                type: "smallint",
                nullable: false,
                defaultValue: (short)3,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldDefaultValue: (short)2);
        }
    }
}
