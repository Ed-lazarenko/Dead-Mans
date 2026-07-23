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
            "game_card_runs",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_game_card_runs_status_allowed",
                    GameCardRunStatusValue.CheckSqlAllowedStatuses
                );
                tableBuilder.HasCheckConstraint(
                    "CK_game_card_runs_finished_at_semantics",
                    GameCardRunStatusValue.CheckSqlFinishedAtSemantics
                );
                tableBuilder.HasCheckConstraint(
                    "CK_game_card_runs_base_score_non_negative",
                    "\"BaseScore\" >= 0"
                );
                tableBuilder.HasCheckConstraint(
                    "CK_game_card_runs_cell_cost_non_negative",
                    "\"CellCostSnapshot\" >= 0"
                );
                tableBuilder.HasCheckConstraint(
                    "CK_game_card_runs_kills_count_non_negative",
                    "\"KillsCount\" >= 0"
                );
                tableBuilder.HasCheckConstraint(
                    "CK_game_card_runs_bounty_count_non_negative",
                    "\"BountyCount\" >= 0"
                );
                tableBuilder.HasCheckConstraint(
                    "CK_game_card_runs_team_slot_non_negative",
                    "\"TeamSlotIndexSnapshot\" >= 0"
                );
                tableBuilder.HasCheckConstraint(
                    "CK_game_card_runs_row_col_non_negative",
                    "\"CellRowIndex\" >= 0 AND \"CellColIndex\" >= 0"
                );
            }
        );

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.CellTitleSnapshot).HasMaxLength(200);
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
            .OnDelete(DeleteBehavior.SetNull);
    }
}
