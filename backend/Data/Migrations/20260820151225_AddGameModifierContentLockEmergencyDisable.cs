using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGameModifierContentLockEmergencyDisable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "emergency_disable_reason",
                table: "game_enabled_modifiers",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "emergency_disabled_at_utc",
                table: "game_enabled_modifiers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "emergency_disabled_by_user_id",
                table: "game_enabled_modifiers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_game_enabled_modifiers_emergency_disabled_by_user_id",
                table: "game_enabled_modifiers",
                column: "emergency_disabled_by_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_enabled_modifiers_emergency_disable_audit",
                table: "game_enabled_modifiers",
                sql: "(emergency_disabled_at_utc IS NULL AND emergency_disabled_by_user_id IS NULL AND emergency_disable_reason IS NULL) OR (emergency_disabled_at_utc IS NOT NULL AND emergency_disabled_by_user_id IS NOT NULL AND emergency_disable_reason IS NOT NULL AND length(btrim(emergency_disable_reason)) BETWEEN 1 AND 1000 AND emergency_disabled_at_utc >= enabled_at_utc)");

            migrationBuilder.AddForeignKey(
                name: "fk_game_enabled_modifiers_users_emergency_disabled_by_user_id",
                table: "game_enabled_modifiers",
                column: "emergency_disabled_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_game_enabled_modifiers_users_emergency_disabled_by_user_id",
                table: "game_enabled_modifiers");

            migrationBuilder.DropIndex(
                name: "ix_game_enabled_modifiers_emergency_disabled_by_user_id",
                table: "game_enabled_modifiers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_game_enabled_modifiers_emergency_disable_audit",
                table: "game_enabled_modifiers");

            migrationBuilder.DropColumn(
                name: "emergency_disable_reason",
                table: "game_enabled_modifiers");

            migrationBuilder.DropColumn(
                name: "emergency_disabled_at_utc",
                table: "game_enabled_modifiers");

            migrationBuilder.DropColumn(
                name: "emergency_disabled_by_user_id",
                table: "game_enabled_modifiers");
        }
    }
}
