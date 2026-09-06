using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGameFinalizationSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "game_finalizations",
                columns: table => new
                {
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    finished_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    finished_by_display_name_snapshot = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    finished_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    public_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    calculation_version = table.Column<int>(type: "integer", nullable: false),
                    completed_round_count = table.Column<int>(type: "integer", nullable: false),
                    cancelled_round_count = table.Column<int>(type: "integer", nullable: false),
                    total_kills = table.Column<int>(type: "integer", nullable: false),
                    total_bounties = table.Column<int>(type: "integer", nullable: false),
                    quiz_total_points = table.Column<int>(type: "integer", nullable: false),
                    skipped_quiz_question_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_finalizations", x => x.game_id);
                    table.CheckConstraint("ck_game_finalizations_calculation_version_positive", "calculation_version > 0");
                    table.CheckConstraint("ck_game_finalizations_counts_non_negative", "completed_round_count >= 0 AND cancelled_round_count >= 0 AND skipped_quiz_question_count >= 0");
                    table.ForeignKey(
                        name: "fk_game_finalizations_games_game_id",
                        column: x => x.game_id,
                        principalTable: "games",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_game_finalizations_users_finished_by_user_id",
                        column: x => x.finished_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "game_team_final_results",
                columns: table => new
                {
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_name_snapshot = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    team_slot_index_snapshot = table.Column<int>(type: "integer", nullable: false),
                    participant_names_snapshot = table.Column<string[]>(type: "text[]", nullable: false),
                    rounds_played = table.Column<int>(type: "integer", nullable: false),
                    best_score = table.Column<int>(type: "integer", nullable: true),
                    penalty_total = table.Column<int>(type: "integer", nullable: false),
                    final_score = table.Column<int>(type: "integer", nullable: true),
                    total_score = table.Column<int>(type: "integer", nullable: false),
                    total_bonus_delta = table.Column<int>(type: "integer", nullable: false),
                    total_kills = table.Column<int>(type: "integer", nullable: false),
                    total_bounties = table.Column<int>(type: "integer", nullable: false),
                    placement = table.Column<int>(type: "integer", nullable: true),
                    last_finished_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_team_final_results", x => new { x.game_id, x.team_id });
                    table.CheckConstraint("ck_game_team_final_results_rounds_non_negative", "rounds_played >= 0");
                    table.CheckConstraint("ck_game_team_final_results_unplayed_semantics", "(rounds_played = 0 AND best_score IS NULL AND final_score IS NULL AND placement IS NULL AND last_finished_at_utc IS NULL) OR (rounds_played > 0 AND best_score IS NOT NULL AND final_score IS NOT NULL AND placement IS NOT NULL AND placement > 0 AND last_finished_at_utc IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_game_team_final_results_game_finalizations_game_id",
                        column: x => x.game_id,
                        principalTable: "game_finalizations",
                        principalColumn: "game_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_game_team_final_results_game_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "game_teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_game_finalizations_finished_by_user_id",
                table: "game_finalizations",
                column: "finished_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_finalizations_request_id",
                table: "game_finalizations",
                column: "request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_game_team_final_results_game_id_placement",
                table: "game_team_final_results",
                columns: new[] { "game_id", "placement" });

            migrationBuilder.CreateIndex(
                name: "ix_game_team_final_results_team_id",
                table: "game_team_final_results",
                column: "team_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "game_team_final_results");

            migrationBuilder.DropTable(
                name: "game_finalizations");
        }
    }
}
