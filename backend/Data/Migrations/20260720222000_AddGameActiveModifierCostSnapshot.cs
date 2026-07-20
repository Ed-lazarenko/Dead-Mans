using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Data.Migrations
{
    public partial class AddGameActiveModifierCostSnapshot : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActivationCostSnapshot",
                table: "game_active_modifiers",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.Sql(
                """
                UPDATE game_active_modifiers AS active
                SET "ActivationCostSnapshot" = modifier."ActivationCost"
                FROM modifier_definitions AS modifier
                WHERE active."ModifierId" = modifier."Id";
                """
            );

            migrationBuilder.AddCheckConstraint(
                name: "CK_game_active_modifiers_activation_cost_non_negative",
                table: "game_active_modifiers",
                sql: "\"ActivationCostSnapshot\" >= 0"
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_game_active_modifiers_activation_cost_non_negative",
                table: "game_active_modifiers"
            );

            migrationBuilder.DropColumn(
                name: "ActivationCostSnapshot",
                table: "game_active_modifiers"
            );
        }
    }
}
