using backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260823100000_EnforceSingleNonterminalGameRound")]
public sealed class EnforceSingleNonterminalGameRound : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM game_rounds
                    WHERE status IN ('awaiting_modifiers','preparing','in_progress','reviewing_results')
                    GROUP BY game_id
                    HAVING COUNT(*) > 1
                ) THEN
                    RAISE EXCEPTION 'A game has more than one nonterminal round; single-round invariant rollout requires manual reconciliation.';
                END IF;
            END $$;
            """
        );

        migrationBuilder.CreateIndex(
            name: "ux_game_rounds_single_nonterminal_game",
            table: "game_rounds",
            column: "game_id",
            unique: true,
            filter: "status IN ('awaiting_modifiers','preparing','in_progress','reviewing_results')"
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ux_game_rounds_single_nonterminal_game",
            table: "game_rounds"
        );
    }
}
