using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Data.Migrations
{
    public partial class LinkTestGameCatalogSelections : Migration
    {
        private const string TestGameId = "c6c6a0da-0bd1-4f0b-bb2f-9a4c9c8b7f6a";
        private const string EnabledAtUtc = "2026-07-20 00:00:00+00";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"""
                INSERT INTO game_modifier_selections ("GameId", "ModifierId", "EnabledAtUtc")
                SELECT
                    '{TestGameId}'::uuid,
                    modifier."Id",
                    TIMESTAMPTZ '{EnabledAtUtc}'
                FROM modifier_definitions AS modifier
                WHERE modifier."IsArchived" = false
                  AND EXISTS (
                      SELECT 1
                      FROM games AS game
                      WHERE game."Id" = '{TestGameId}'::uuid
                  )
                  AND NOT EXISTS (
                      SELECT 1
                      FROM game_modifier_selections AS selection
                      WHERE selection."GameId" = '{TestGameId}'::uuid
                        AND selection."ModifierId" = modifier."Id"
                  );

                INSERT INTO game_question_selections ("GameId", "QuestionId", "EnabledAtUtc")
                SELECT
                    '{TestGameId}'::uuid,
                    question."Id",
                    TIMESTAMPTZ '{EnabledAtUtc}'
                FROM question_definitions AS question
                WHERE question."IsDeleted" = false
                  AND EXISTS (
                      SELECT 1
                      FROM games AS game
                      WHERE game."Id" = '{TestGameId}'::uuid
                  )
                  AND NOT EXISTS (
                      SELECT 1
                      FROM game_question_selections AS selection
                      WHERE selection."GameId" = '{TestGameId}'::uuid
                        AND selection."QuestionId" = question."Id"
                  );
                """
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"""
                DELETE FROM game_modifier_selections AS selection
                USING modifier_definitions AS modifier
                WHERE selection."GameId" = '{TestGameId}'::uuid
                  AND selection."ModifierId" = modifier."Id"
                  AND modifier."IsArchived" = false;

                DELETE FROM game_question_selections AS selection
                USING question_definitions AS question
                WHERE selection."GameId" = '{TestGameId}'::uuid
                  AND selection."QuestionId" = question."Id"
                  AND question."IsDeleted" = false;
                """
            );
        }
    }
}
