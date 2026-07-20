using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Data.Migrations
{
    public partial class AddAwaitingModifiersCardRunStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_game_card_runs_finished_at_semantics",
                table: "game_card_runs"
            );
            migrationBuilder.DropCheckConstraint(
                name: "CK_game_card_runs_status_allowed",
                table: "game_card_runs"
            );

            migrationBuilder.AddCheckConstraint(
                name: "CK_game_card_runs_status_allowed",
                table: "game_card_runs",
                sql: "\"Status\" IN ('awaiting_modifiers','in_progress','completed','cancelled')"
            );
            migrationBuilder.AddCheckConstraint(
                name: "CK_game_card_runs_finished_at_semantics",
                table: "game_card_runs",
                sql: "((\"Status\" IN ('awaiting_modifiers','in_progress')) AND \"FinishedAtUtc\" IS NULL) OR ((\"Status\" IN ('completed','cancelled')) AND \"FinishedAtUtc\" IS NOT NULL)"
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_game_card_runs_finished_at_semantics",
                table: "game_card_runs"
            );
            migrationBuilder.DropCheckConstraint(
                name: "CK_game_card_runs_status_allowed",
                table: "game_card_runs"
            );

            migrationBuilder.AddCheckConstraint(
                name: "CK_game_card_runs_status_allowed",
                table: "game_card_runs",
                sql: "\"Status\" IN ('in_progress','completed','cancelled')"
            );
            migrationBuilder.AddCheckConstraint(
                name: "CK_game_card_runs_finished_at_semantics",
                table: "game_card_runs",
                sql: "((\"Status\" = 'in_progress') AND \"FinishedAtUtc\" IS NULL) OR ((\"Status\" IN ('completed','cancelled')) AND \"FinishedAtUtc\" IS NOT NULL)"
            );
        }
    }
}
