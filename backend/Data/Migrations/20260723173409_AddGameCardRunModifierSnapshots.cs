using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGameCardRunModifierSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModifierDescriptionSnapshot",
                table: "game_card_run_modifier_results",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ModifierEffectSnapshotJson",
                table: "game_card_run_modifier_results",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifierScoringTypeSnapshot",
                table: "game_card_run_modifier_results",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModifierDescriptionSnapshot",
                table: "game_card_run_modifier_results");

            migrationBuilder.DropColumn(
                name: "ModifierEffectSnapshotJson",
                table: "game_card_run_modifier_results");

            migrationBuilder.DropColumn(
                name: "ModifierScoringTypeSnapshot",
                table: "game_card_run_modifier_results");
        }
    }
}
