using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRoundRebuildTechnicalCancellationAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_game_board_cells_state_allowed",
                table: "game_board_cells");

            migrationBuilder.AddColumn<string>(
                name: "internal_cancellation_detail",
                table: "game_rounds",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "public_cancellation_summary",
                table: "game_rounds",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "technical_cancellation_reason_code",
                table: "game_rounds",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE game_rounds
                SET technical_cancellation_reason_code = 'application_error',
                    internal_cancellation_detail = 'Legacy cancellation migrated without structured technical detail.'
                WHERE status = 'cancelled'
                  AND technical_cancellation_reason_code IS NULL;
                """
            );

            migrationBuilder.CreateTable(
                name: "game_round_transition_audits",
                columns: table => new
                {
                    round_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    from_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    to_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    action_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    initiated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    resulting_round_version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_round_transition_audits", x => new { x.round_id, x.sequence });
                    table.CheckConstraint("ck_game_round_transition_audits_resulting_version_positive", "resulting_round_version > 0");
                    table.CheckConstraint("ck_game_round_transition_audits_sequence_positive", "sequence > 0");
                    table.ForeignKey(
                        name: "fk_game_round_transition_audits_game_rounds_round_id",
                        column: x => x.round_id,
                        principalTable: "game_rounds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_game_round_transition_audits_users_initiated_by_user_id",
                        column: x => x.initiated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_rounds_technical_cancellation_semantics",
                table: "game_rounds",
                sql: "(status = 'cancelled' AND technical_cancellation_reason_code IS NOT NULL AND internal_cancellation_detail IS NOT NULL AND (technical_cancellation_reason_code <> 'other' OR public_cancellation_summary IS NOT NULL)) OR (status <> 'cancelled' AND technical_cancellation_reason_code IS NULL AND public_cancellation_summary IS NULL AND internal_cancellation_detail IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_rounds_technical_cancellation_reason_allowed",
                table: "game_rounds",
                sql: "technical_cancellation_reason_code IS NULL OR technical_cancellation_reason_code IN ('external_game_failure','stream_or_infrastructure_failure','application_error','operator_error','other')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_board_cells_state_allowed",
                table: "game_board_cells",
                sql: "state IN ('open','closed','cancelled')");

            migrationBuilder.CreateIndex(
                name: "ix_game_round_transition_audits_initiated_by_user_id",
                table: "game_round_transition_audits",
                column: "initiated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_round_transition_audits_round_id_resulting_round_versi~",
                table: "game_round_transition_audits",
                columns: new[] { "round_id", "resulting_round_version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "game_round_transition_audits");

            migrationBuilder.DropCheckConstraint(
                name: "ck_game_rounds_technical_cancellation_semantics",
                table: "game_rounds");

            migrationBuilder.DropCheckConstraint(
                name: "ck_game_rounds_technical_cancellation_reason_allowed",
                table: "game_rounds");

            migrationBuilder.DropCheckConstraint(
                name: "ck_game_board_cells_state_allowed",
                table: "game_board_cells");

            migrationBuilder.DropColumn(
                name: "internal_cancellation_detail",
                table: "game_rounds");

            migrationBuilder.DropColumn(
                name: "public_cancellation_summary",
                table: "game_rounds");

            migrationBuilder.DropColumn(
                name: "technical_cancellation_reason_code",
                table: "game_rounds");

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_board_cells_state_allowed",
                table: "game_board_cells",
                sql: "state IN ('open','closed')");
        }
    }
}
