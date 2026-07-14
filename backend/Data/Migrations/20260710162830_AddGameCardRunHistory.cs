using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGameCardRunHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "game_card_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    BoardCellId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BaseScore = table.Column<int>(type: "integer", nullable: false),
                    FinalScore = table.Column<int>(type: "integer", nullable: true),
                    TeamSlotIndexSnapshot = table.Column<int>(type: "integer", nullable: false),
                    CellRowIndex = table.Column<int>(type: "integer", nullable: false),
                    CellColIndex = table.Column<int>(type: "integer", nullable: false),
                    CellTitleSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CellCostSnapshot = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_card_runs", x => x.Id);
                    table.CheckConstraint("CK_game_card_runs_base_score_non_negative", "\"BaseScore\" >= 0");
                    table.CheckConstraint("CK_game_card_runs_cell_cost_non_negative", "\"CellCostSnapshot\" >= 0");
                    table.CheckConstraint("CK_game_card_runs_finished_at_semantics", "((\"Status\" = 'in_progress') AND \"FinishedAtUtc\" IS NULL) OR ((\"Status\" IN ('completed','cancelled')) AND \"FinishedAtUtc\" IS NOT NULL)");
                    table.CheckConstraint("CK_game_card_runs_row_col_non_negative", "\"CellRowIndex\" >= 0 AND \"CellColIndex\" >= 0");
                    table.CheckConstraint("CK_game_card_runs_status_allowed", "\"Status\" IN ('in_progress','completed','cancelled')");
                    table.CheckConstraint("CK_game_card_runs_team_slot_non_negative", "\"TeamSlotIndexSnapshot\" >= 0");
                    table.ForeignKey(
                        name: "FK_game_card_runs_board_cells_BoardCellId",
                        column: x => x.BoardCellId,
                        principalTable: "board_cells",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_game_card_runs_game_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "game_teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_game_card_runs_games_GameId",
                        column: x => x.GameId,
                        principalTable: "games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_game_card_runs_users_ResolvedByUserId",
                        column: x => x.ResolvedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "game_card_run_modifier_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CardRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    GameActiveModifierId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifierId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifierNameSnapshot = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ModifierCategorySnapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ModifierMechanicTypeSnapshot = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OutcomeStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ScoreDelta = table.Column<int>(type: "integer", nullable: false),
                    KillDelta = table.Column<int>(type: "integer", nullable: false),
                    MultiplierApplied = table.Column<decimal>(type: "numeric", nullable: true),
                    ResolutionDataJson = table.Column<string>(type: "jsonb", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_card_run_modifier_results", x => x.Id);
                    table.CheckConstraint("CK_game_card_run_modifier_results_status_allowed", "\"OutcomeStatus\" IN ('pending','completed','failed','cancelled')");
                    table.ForeignKey(
                        name: "FK_game_card_run_modifier_results_game_active_modifiers_GameAc~",
                        column: x => x.GameActiveModifierId,
                        principalTable: "game_active_modifiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_game_card_run_modifier_results_game_card_runs_CardRunId",
                        column: x => x.CardRunId,
                        principalTable: "game_card_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_game_card_run_modifier_results_modifier_definitions_Modifie~",
                        column: x => x.ModifierId,
                        principalTable: "modifier_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_game_card_run_modifier_results_users_ResolvedByUserId",
                        column: x => x.ResolvedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "game_card_run_participants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CardRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayNameSnapshot = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_card_run_participants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_game_card_run_participants_game_card_runs_CardRunId",
                        column: x => x.CardRunId,
                        principalTable: "game_card_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_game_card_run_participants_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_game_card_run_modifier_results_CardRunId_GameActiveModifier~",
                table: "game_card_run_modifier_results",
                columns: new[] { "CardRunId", "GameActiveModifierId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_game_card_run_modifier_results_CardRunId_OutcomeStatus",
                table: "game_card_run_modifier_results",
                columns: new[] { "CardRunId", "OutcomeStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_game_card_run_modifier_results_GameActiveModifierId",
                table: "game_card_run_modifier_results",
                column: "GameActiveModifierId");

            migrationBuilder.CreateIndex(
                name: "IX_game_card_run_modifier_results_ModifierId_OutcomeStatus",
                table: "game_card_run_modifier_results",
                columns: new[] { "ModifierId", "OutcomeStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_game_card_run_modifier_results_ResolvedByUserId",
                table: "game_card_run_modifier_results",
                column: "ResolvedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_game_card_run_participants_CardRunId_UserId",
                table: "game_card_run_participants",
                columns: new[] { "CardRunId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_game_card_run_participants_UserId_CreatedAtUtc",
                table: "game_card_run_participants",
                columns: new[] { "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_game_card_runs_BoardCellId_StartedAtUtc",
                table: "game_card_runs",
                columns: new[] { "BoardCellId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_game_card_runs_GameId_StartedAtUtc",
                table: "game_card_runs",
                columns: new[] { "GameId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_game_card_runs_GameId_TeamId_BoardCellId_StartedAtUtc",
                table: "game_card_runs",
                columns: new[] { "GameId", "TeamId", "BoardCellId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_game_card_runs_ResolvedByUserId",
                table: "game_card_runs",
                column: "ResolvedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_game_card_runs_TeamId_StartedAtUtc",
                table: "game_card_runs",
                columns: new[] { "TeamId", "StartedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "game_card_run_modifier_results");

            migrationBuilder.DropTable(
                name: "game_card_run_participants");

            migrationBuilder.DropTable(
                name: "game_card_runs");
        }
    }
}
