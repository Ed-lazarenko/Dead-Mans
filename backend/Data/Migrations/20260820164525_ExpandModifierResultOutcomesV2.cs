using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExpandModifierResultOutcomesV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_game_round_modifier_results_status_allowed",
                table: "game_round_modifier_results");

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_round_modifier_results_status_allowed",
                table: "game_round_modifier_results",
                sql: "outcome_status IN ('pending','completed','failed','cancelled','violated','not_triggered','succeeded','not_succeeded','calculated')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $migration$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM game_round_modifier_results
                        WHERE outcome_status IN (
                            'violated',
                            'not_triggered',
                            'succeeded',
                            'not_succeeded',
                            'calculated'
                        )
                    ) THEN
                        RAISE EXCEPTION USING
                            ERRCODE = 'check_violation',
                            MESSAGE = 'BehaviorV2 outcome rollback requires manual reconciliation.';
                    END IF;
                END $migration$;
                """
            );

            migrationBuilder.DropCheckConstraint(
                name: "ck_game_round_modifier_results_status_allowed",
                table: "game_round_modifier_results");

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_round_modifier_results_status_allowed",
                table: "game_round_modifier_results",
                sql: "outcome_status IN ('pending','completed','failed','cancelled')");
        }
    }
}
