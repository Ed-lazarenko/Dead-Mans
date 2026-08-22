using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class GameRoundConfiguration : IEntityTypeConfiguration<GameRound>
{
    public void Configure(EntityTypeBuilder<GameRound> builder)
    {
        builder.ToTable(
            "game_rounds",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_game_rounds_status_allowed",
                    GameRoundStatusValue.CheckSqlAllowedStatuses
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_rounds_finished_at_semantics",
                    GameRoundStatusValue.CheckSqlFinishedAtSemantics
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_rounds_resolution_semantics",
                    "((status IN ('awaiting_modifiers','preparing','in_progress','reviewing_results')) AND final_score IS NULL AND resolved_by_user_id IS NULL) "
                    + "OR ((status = 'completed') AND final_score IS NOT NULL AND resolved_by_user_id IS NOT NULL) "
                    + "OR ((status = 'cancelled') AND final_score = 0 AND resolved_by_user_id IS NOT NULL)"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_rounds_empty_card_penalty_semantics",
                    "(empty_card_penalty_applied = false) OR (status = 'completed' AND final_score IS NOT NULL)"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_rounds_version_positive",
                    "version > 0"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_rounds_lifecycle_timestamps",
                    "(status = 'awaiting_modifiers' AND prepared_at_utc IS NULL AND gameplay_started_at_utc IS NULL AND reviewed_at_utc IS NULL) "
                    + "OR (status = 'preparing' AND prepared_at_utc IS NOT NULL AND gameplay_started_at_utc IS NULL AND reviewed_at_utc IS NULL) "
                    + "OR (status = 'in_progress' AND prepared_at_utc IS NOT NULL AND gameplay_started_at_utc IS NOT NULL AND reviewed_at_utc IS NULL) "
                    + "OR (status = 'reviewing_results' AND prepared_at_utc IS NOT NULL AND gameplay_started_at_utc IS NOT NULL AND reviewed_at_utc IS NOT NULL) "
                    + "OR (status IN ('completed','cancelled'))"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_rounds_base_score_non_negative",
                    "base_score >= 0"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_rounds_cell_cost_non_negative",
                    "cell_cost_snapshot >= 0"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_rounds_kills_count_non_negative",
                    "kills_count >= 0"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_rounds_bounty_count_non_negative",
                    "bounty_count >= 0"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_rounds_team_slot_non_negative",
                    "team_slot_index_snapshot >= 0"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_rounds_row_col_non_negative",
                    "cell_row_index >= 0 AND cell_col_index >= 0"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_rounds_technical_cancellation_semantics",
                    "(status = 'cancelled' AND technical_cancellation_reason_code IS NOT NULL "
                    + "AND internal_cancellation_detail IS NOT NULL "
                    + "AND (technical_cancellation_reason_code <> 'other' OR public_cancellation_summary IS NOT NULL)) "
                    + "OR (status <> 'cancelled' "
                    + "AND technical_cancellation_reason_code IS NULL AND public_cancellation_summary IS NULL "
                    + "AND internal_cancellation_detail IS NULL)"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_rounds_technical_cancellation_reason_allowed",
                    "technical_cancellation_reason_code IS NULL OR technical_cancellation_reason_code IN "
                    + "('external_game_failure','stream_or_infrastructure_failure','application_error','operator_error','other')"
                );
            }
        );

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.CellTitleSnapshot).HasMaxLength(200);
        builder.Property(x => x.CellDescriptionSnapshot).HasMaxLength(2000);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.TechnicalCancellationReasonCode).HasMaxLength(64);
        builder.Property(x => x.PublicCancellationSummary).HasMaxLength(500);
        builder.Property(x => x.InternalCancellationDetail).HasMaxLength(2000);
        builder.Property(x => x.KillsCount).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.BountyCount).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.EmptyCardPenaltyApplied).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.Version).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.StartedAtUtc).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.GameId, x.StartedAtUtc });
        builder
            .HasIndex(x => x.GameId, "ux_game_rounds_single_nonterminal_game")
            .IsUnique()
            .HasFilter(
                "status IN ('awaiting_modifiers','preparing','in_progress','reviewing_results')"
            );
        builder.HasIndex(x => new { x.TeamId, x.StartedAtUtc });
        builder.HasIndex(x => new { x.BoardCellId, x.StartedAtUtc });
        builder.HasIndex(x => new { x.GameId, x.TeamId, x.BoardCellId, x.StartedAtUtc });

        builder
            .HasOne(x => x.Game)
            .WithMany()
            .HasForeignKey(x => x.GameId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.BoardCell)
            .WithMany()
            .HasForeignKey(x => x.BoardCellId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Team)
            .WithMany()
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.ResolvedByUser)
            .WithMany()
            .HasForeignKey(x => x.ResolvedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
