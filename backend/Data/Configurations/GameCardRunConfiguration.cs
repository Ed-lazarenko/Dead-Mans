using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class GameCardRunConfiguration : IEntityTypeConfiguration<GameCardRun>
{
    public void Configure(EntityTypeBuilder<GameCardRun> builder)
    {
        builder.ToTable(
            "game_rounds",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_game_rounds_status_allowed",
                    GameCardRunStatusValue.CheckSqlAllowedStatuses
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_rounds_finished_at_semantics",
                    GameCardRunStatusValue.CheckSqlFinishedAtSemantics
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_rounds_resolution_semantics",
                    "((status IN ('awaiting_modifiers','in_progress','reviewing_results')) AND final_score IS NULL AND resolved_by_user_id IS NULL) "
                    + "OR ((status = 'completed') AND final_score IS NOT NULL AND resolved_by_user_id IS NOT NULL) "
                    + "OR ((status = 'cancelled') AND final_score = 0 AND resolved_by_user_id IS NOT NULL)"
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
            }
        );

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.CellTitleSnapshot).HasMaxLength(200);
        builder.Property(x => x.CellDescriptionSnapshot).HasMaxLength(2000);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.KillsCount).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.BountyCount).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.StartedAtUtc).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.GameId, x.StartedAtUtc });
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
