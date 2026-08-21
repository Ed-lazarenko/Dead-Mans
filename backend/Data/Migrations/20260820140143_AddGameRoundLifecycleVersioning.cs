using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGameRoundLifecycleVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_game_rounds_finished_at_semantics",
                table: "game_rounds");

            migrationBuilder.DropCheckConstraint(
                name: "ck_game_rounds_resolution_semantics",
                table: "game_rounds");

            migrationBuilder.DropCheckConstraint(
                name: "ck_game_rounds_status_allowed",
                table: "game_rounds");

            migrationBuilder.AddColumn<DateTime>(
                name: "gameplay_started_at_utc",
                table: "game_rounds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "prepared_at_utc",
                table: "game_rounds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reviewed_at_utc",
                table: "game_rounds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "game_rounds",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(
                """
                UPDATE game_rounds
                SET prepared_at_utc = started_at_utc,
                    gameplay_started_at_utc = started_at_utc,
                    reviewed_at_utc = CASE
                        WHEN status = 'reviewing_results' THEN updated_at_utc
                        WHEN status = 'completed' THEN COALESCE(finished_at_utc, updated_at_utc)
                        ELSE NULL
                    END
                WHERE status IN ('in_progress', 'reviewing_results', 'completed', 'cancelled');
                """
            );

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_rounds_finished_at_semantics",
                table: "game_rounds",
                sql: "((status IN ('awaiting_modifiers','preparing','in_progress','reviewing_results')) AND finished_at_utc IS NULL) OR ((status IN ('completed','cancelled')) AND finished_at_utc IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_rounds_lifecycle_timestamps",
                table: "game_rounds",
                sql: "(status = 'awaiting_modifiers' AND prepared_at_utc IS NULL AND gameplay_started_at_utc IS NULL AND reviewed_at_utc IS NULL) OR (status = 'preparing' AND prepared_at_utc IS NOT NULL AND gameplay_started_at_utc IS NULL AND reviewed_at_utc IS NULL) OR (status = 'in_progress' AND prepared_at_utc IS NOT NULL AND gameplay_started_at_utc IS NOT NULL AND reviewed_at_utc IS NULL) OR (status = 'reviewing_results' AND prepared_at_utc IS NOT NULL AND gameplay_started_at_utc IS NOT NULL AND reviewed_at_utc IS NOT NULL) OR (status IN ('completed','cancelled'))");

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_rounds_resolution_semantics",
                table: "game_rounds",
                sql: "((status IN ('awaiting_modifiers','preparing','in_progress','reviewing_results')) AND final_score IS NULL AND resolved_by_user_id IS NULL) OR ((status = 'completed') AND final_score IS NOT NULL AND resolved_by_user_id IS NOT NULL) OR ((status = 'cancelled') AND final_score = 0 AND resolved_by_user_id IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_rounds_status_allowed",
                table: "game_rounds",
                sql: "status IN ('awaiting_modifiers','preparing','in_progress','reviewing_results','completed','cancelled')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_rounds_version_positive",
                table: "game_rounds",
                sql: "version > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_game_rounds_finished_at_semantics",
                table: "game_rounds");

            migrationBuilder.DropCheckConstraint(
                name: "ck_game_rounds_lifecycle_timestamps",
                table: "game_rounds");

            migrationBuilder.DropCheckConstraint(
                name: "ck_game_rounds_resolution_semantics",
                table: "game_rounds");

            migrationBuilder.DropCheckConstraint(
                name: "ck_game_rounds_status_allowed",
                table: "game_rounds");

            migrationBuilder.DropCheckConstraint(
                name: "ck_game_rounds_version_positive",
                table: "game_rounds");

            migrationBuilder.Sql(
                """
                UPDATE game_rounds
                SET status = 'awaiting_modifiers',
                    prepared_at_utc = NULL
                WHERE status = 'preparing';
                """
            );

            migrationBuilder.DropColumn(
                name: "gameplay_started_at_utc",
                table: "game_rounds");

            migrationBuilder.DropColumn(
                name: "prepared_at_utc",
                table: "game_rounds");

            migrationBuilder.DropColumn(
                name: "reviewed_at_utc",
                table: "game_rounds");

            migrationBuilder.DropColumn(
                name: "version",
                table: "game_rounds");

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_rounds_finished_at_semantics",
                table: "game_rounds",
                sql: "((status IN ('awaiting_modifiers','in_progress','reviewing_results')) AND finished_at_utc IS NULL) OR ((status IN ('completed','cancelled')) AND finished_at_utc IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_rounds_resolution_semantics",
                table: "game_rounds",
                sql: "((status IN ('awaiting_modifiers','in_progress','reviewing_results')) AND final_score IS NULL AND resolved_by_user_id IS NULL) OR ((status = 'completed') AND final_score IS NOT NULL AND resolved_by_user_id IS NOT NULL) OR ((status = 'cancelled') AND final_score = 0 AND resolved_by_user_id IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_rounds_status_allowed",
                table: "game_rounds",
                sql: "status IN ('awaiting_modifiers','in_progress','reviewing_results','completed','cancelled')");
        }
    }
}
