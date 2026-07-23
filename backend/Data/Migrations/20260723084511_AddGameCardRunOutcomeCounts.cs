using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGameCardRunOutcomeCounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BountyCount",
                table: "game_card_runs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "KillsCount",
                table: "game_card_runs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_game_card_runs_bounty_count_non_negative",
                table: "game_card_runs",
                sql: "\"BountyCount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_game_card_runs_kills_count_non_negative",
                table: "game_card_runs",
                sql: "\"KillsCount\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_game_card_runs_bounty_count_non_negative",
                table: "game_card_runs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_game_card_runs_kills_count_non_negative",
                table: "game_card_runs");

            migrationBuilder.DropColumn(
                name: "BountyCount",
                table: "game_card_runs");

            migrationBuilder.DropColumn(
                name: "KillsCount",
                table: "game_card_runs");
        }
    }
}
