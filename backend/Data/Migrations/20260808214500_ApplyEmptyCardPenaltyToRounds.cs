using backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260808214500_ApplyEmptyCardPenaltyToRounds")]
    public partial class ApplyEmptyCardPenaltyToRounds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "empty_card_penalty_applied",
                table: "game_rounds",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_rounds_empty_card_penalty_semantics",
                table: "game_rounds",
                sql: "(empty_card_penalty_applied = false) OR (status = 'completed' AND final_score IS NOT NULL)");

            migrationBuilder.Sql(
                """
                UPDATE game_rounds AS round
                SET
                    empty_card_penalty_applied =
                        round.status = 'completed'
                        AND round.base_score > 0
                        AND (score_parts.base_actions_count * round.base_score) <= 0
                        AND score_parts.modifier_score_delta <= 0,
                    final_score =
                    CASE
                        WHEN round.status = 'cancelled' THEN 0
                        WHEN round.base_score > 0
                            AND (score_parts.base_actions_count * round.base_score) <= 0
                            AND score_parts.modifier_score_delta <= 0
                            THEN (score_parts.base_actions_count * round.base_score)
                                + score_parts.modifier_score_delta
                                - round.base_score
                        ELSE (score_parts.base_actions_count * round.base_score)
                            + score_parts.modifier_score_delta
                    END
                FROM (
                    SELECT
                        round_source.id,
                        round_source.kills_count
                            + round_source.bounty_count
                            + COALESCE(SUM(modifier.kill_delta), 0) AS base_actions_count,
                        COALESCE(SUM(modifier.score_delta), 0) AS modifier_score_delta
                    FROM game_rounds AS round_source
                    LEFT JOIN game_round_modifier_results AS modifier
                        ON modifier.round_id = round_source.id
                    GROUP BY round_source.id
                ) AS score_parts
                WHERE round.id = score_parts.id
                    AND round.finished_at_utc IS NOT NULL
                    AND round.status IN ('completed', 'cancelled');
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE game_rounds AS round
                SET final_score =
                    CASE
                        WHEN round.status = 'cancelled' THEN 0
                        ELSE (score_parts.base_actions_count * round.base_score)
                            + score_parts.modifier_score_delta
                    END
                FROM (
                    SELECT
                        round_source.id,
                        round_source.kills_count
                            + round_source.bounty_count
                            + COALESCE(SUM(modifier.kill_delta), 0) AS base_actions_count,
                        COALESCE(SUM(modifier.score_delta), 0) AS modifier_score_delta
                    FROM game_rounds AS round_source
                    LEFT JOIN game_round_modifier_results AS modifier
                        ON modifier.round_id = round_source.id
                    GROUP BY round_source.id
                ) AS score_parts
                WHERE round.id = score_parts.id
                    AND round.finished_at_utc IS NOT NULL
                    AND round.status IN ('completed', 'cancelled');
                """
            );

            migrationBuilder.DropCheckConstraint(
                name: "ck_game_rounds_empty_card_penalty_semantics",
                table: "game_rounds");

            migrationBuilder.DropColumn(
                name: "empty_card_penalty_applied",
                table: "game_rounds");
        }
    }
}
