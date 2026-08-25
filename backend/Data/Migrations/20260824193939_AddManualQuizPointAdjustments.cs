using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddManualQuizPointAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_game_quiz_manual_awards_points_positive",
                table: "game_quiz_manual_awards");

            migrationBuilder.AddColumn<int>(
                name: "available_points_after",
                table: "game_quiz_manual_awards",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "available_points_before",
                table: "game_quiz_manual_awards",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "operation_type",
                table: "game_quiz_manual_awards",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "award");

            migrationBuilder.AddColumn<string>(
                name: "reason",
                table: "game_quiz_manual_awards",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "request_id",
                table: "game_quiz_manual_awards",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_game_quiz_manual_awards_request_id",
                table: "game_quiz_manual_awards",
                column: "request_id",
                unique: true,
                filter: "request_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_quiz_manual_awards_adjustment_audit",
                table: "game_quiz_manual_awards",
                sql: "request_id IS NULL OR (reason IS NOT NULL AND length(trim(reason)) BETWEEN 3 AND 500 AND available_points_before IS NOT NULL AND available_points_after IS NOT NULL AND available_points_after >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_quiz_manual_awards_operation_sign",
                table: "game_quiz_manual_awards",
                sql: "(operation_type = 'award' AND points > 0) OR (operation_type = 'deduct' AND points < 0)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_quiz_manual_awards_operation_type",
                table: "game_quiz_manual_awards",
                sql: "operation_type IN ('award', 'deduct')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_quiz_manual_awards_points_nonzero",
                table: "game_quiz_manual_awards",
                sql: "points <> 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_game_quiz_manual_awards_request_id",
                table: "game_quiz_manual_awards");

            migrationBuilder.DropCheckConstraint(
                name: "ck_game_quiz_manual_awards_adjustment_audit",
                table: "game_quiz_manual_awards");

            migrationBuilder.DropCheckConstraint(
                name: "ck_game_quiz_manual_awards_operation_sign",
                table: "game_quiz_manual_awards");

            migrationBuilder.DropCheckConstraint(
                name: "ck_game_quiz_manual_awards_operation_type",
                table: "game_quiz_manual_awards");

            migrationBuilder.DropCheckConstraint(
                name: "ck_game_quiz_manual_awards_points_nonzero",
                table: "game_quiz_manual_awards");

            migrationBuilder.DropColumn(
                name: "available_points_after",
                table: "game_quiz_manual_awards");

            migrationBuilder.DropColumn(
                name: "available_points_before",
                table: "game_quiz_manual_awards");

            migrationBuilder.DropColumn(
                name: "operation_type",
                table: "game_quiz_manual_awards");

            migrationBuilder.DropColumn(
                name: "reason",
                table: "game_quiz_manual_awards");

            migrationBuilder.DropColumn(
                name: "request_id",
                table: "game_quiz_manual_awards");

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_quiz_manual_awards_points_positive",
                table: "game_quiz_manual_awards",
                sql: "points > 0");
        }
    }
}
