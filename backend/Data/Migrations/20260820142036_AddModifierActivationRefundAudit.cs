using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddModifierActivationRefundAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cancellation_reason",
                table: "game_modifier_activations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "cancelled_at_utc",
                table: "game_modifier_activations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "cancelled_by_user_id",
                table: "game_modifier_activations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid?>(
                name: "initiated_by_user_id",
                table: "game_modifier_activations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "refund_amount",
                table: "game_modifier_activations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid?>(
                name: "round_id",
                table: "game_modifier_activations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "game_modifier_activations",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT modifier_activation_id
                        FROM game_round_modifier_results
                        GROUP BY modifier_activation_id
                        HAVING COUNT(DISTINCT round_id) > 1
                    ) THEN
                        RAISE EXCEPTION 'Modifier activation is linked to more than one round; refund-audit rollout requires manual reconciliation.';
                    END IF;
                END $$;

                UPDATE game_modifier_activations AS activation
                SET round_id = (
                    SELECT result.round_id
                    FROM game_round_modifier_results AS result
                    WHERE result.modifier_activation_id = activation.id
                    ORDER BY result.round_id
                    LIMIT 1
                )
                WHERE EXISTS (
                    SELECT 1
                    FROM game_round_modifier_results AS result
                    WHERE result.modifier_activation_id = activation.id
                );

                UPDATE game_modifier_activations AS activation
                SET round_id = (
                    SELECT round.id
                    FROM game_rounds AS round
                    WHERE round.game_id = activation.game_id
                      AND round.status IN ('awaiting_modifiers','preparing','in_progress','reviewing_results')
                    ORDER BY round.id
                    LIMIT 1
                )
                WHERE activation.round_id IS NULL
                  AND (
                      SELECT COUNT(*)
                      FROM game_rounds AS round
                      WHERE round.game_id = activation.game_id
                        AND round.status IN ('awaiting_modifiers','preparing','in_progress','reviewing_results')
                  ) = 1;

                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM game_modifier_activations
                        WHERE round_id IS NULL
                    ) THEN
                        RAISE EXCEPTION 'Modifier activation cannot be mapped unambiguously to a round; refund-audit rollout requires manual reconciliation.';
                    END IF;
                END $$;

                UPDATE game_modifier_activations
                SET initiated_by_user_id = activated_by_user_id,
                    status = CASE
                        WHEN archived_at_utc IS NOT NULL
                          OR EXISTS (
                              SELECT 1
                              FROM game_round_modifier_results AS result
                              WHERE result.modifier_activation_id = game_modifier_activations.id
                          )
                        THEN 'consumed'
                        ELSE 'active'
                    END;
                """
            );

            migrationBuilder.AlterColumn<Guid>(
                name: "initiated_by_user_id",
                table: "game_modifier_activations",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "round_id",
                table: "game_modifier_activations",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "game_modifier_activations",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_game_modifier_activations_cancelled_by_user_id",
                table: "game_modifier_activations",
                column: "cancelled_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_modifier_activations_initiated_by_user_id",
                table: "game_modifier_activations",
                column: "initiated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_modifier_activations_round_status_activated",
                table: "game_modifier_activations",
                columns: new[] { "round_id", "status", "activated_at_utc" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_modifier_activations_lifecycle_semantics",
                table: "game_modifier_activations",
                sql: "(status = 'active' AND archived_at_utc IS NULL AND cancelled_at_utc IS NULL AND cancelled_by_user_id IS NULL AND cancellation_reason IS NULL AND refund_amount = 0) OR (status = 'consumed' AND cancelled_at_utc IS NULL AND cancelled_by_user_id IS NULL AND cancellation_reason IS NULL AND refund_amount = 0) OR (status = 'cancelled' AND archived_at_utc IS NOT NULL AND cancelled_at_utc IS NOT NULL AND cancelled_by_user_id IS NOT NULL AND refund_amount = activation_cost_snapshot)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_modifier_activations_refund_range",
                table: "game_modifier_activations",
                sql: "refund_amount >= 0 AND refund_amount <= activation_cost_snapshot");

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_modifier_activations_status_allowed",
                table: "game_modifier_activations",
                sql: "status IN ('active','consumed','cancelled')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_modifier_activations_timestamp_order",
                table: "game_modifier_activations",
                sql: "(archived_at_utc IS NULL OR archived_at_utc >= activated_at_utc) AND (cancelled_at_utc IS NULL OR (cancelled_at_utc >= activated_at_utc AND archived_at_utc = cancelled_at_utc))");

            migrationBuilder.AddForeignKey(
                name: "fk_game_modifier_activations_game_rounds_round_id",
                table: "game_modifier_activations",
                column: "round_id",
                principalTable: "game_rounds",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_game_modifier_activations_users_cancelled_by_user_id",
                table: "game_modifier_activations",
                column: "cancelled_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_game_modifier_activations_users_initiated_by_user_id",
                table: "game_modifier_activations",
                column: "initiated_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_game_modifier_activations_game_rounds_round_id",
                table: "game_modifier_activations");

            migrationBuilder.DropForeignKey(
                name: "fk_game_modifier_activations_users_cancelled_by_user_id",
                table: "game_modifier_activations");

            migrationBuilder.DropForeignKey(
                name: "fk_game_modifier_activations_users_initiated_by_user_id",
                table: "game_modifier_activations");

            migrationBuilder.DropIndex(
                name: "ix_game_modifier_activations_cancelled_by_user_id",
                table: "game_modifier_activations");

            migrationBuilder.DropIndex(
                name: "ix_game_modifier_activations_initiated_by_user_id",
                table: "game_modifier_activations");

            migrationBuilder.DropIndex(
                name: "ix_game_modifier_activations_round_status_activated",
                table: "game_modifier_activations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_game_modifier_activations_lifecycle_semantics",
                table: "game_modifier_activations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_game_modifier_activations_refund_range",
                table: "game_modifier_activations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_game_modifier_activations_status_allowed",
                table: "game_modifier_activations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_game_modifier_activations_timestamp_order",
                table: "game_modifier_activations");

            migrationBuilder.DropColumn(
                name: "cancellation_reason",
                table: "game_modifier_activations");

            migrationBuilder.DropColumn(
                name: "cancelled_at_utc",
                table: "game_modifier_activations");

            migrationBuilder.DropColumn(
                name: "cancelled_by_user_id",
                table: "game_modifier_activations");

            migrationBuilder.DropColumn(
                name: "initiated_by_user_id",
                table: "game_modifier_activations");

            migrationBuilder.DropColumn(
                name: "refund_amount",
                table: "game_modifier_activations");

            migrationBuilder.DropColumn(
                name: "round_id",
                table: "game_modifier_activations");

            migrationBuilder.DropColumn(
                name: "status",
                table: "game_modifier_activations");
        }
    }
}
