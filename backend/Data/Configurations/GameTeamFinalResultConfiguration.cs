using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public sealed class GameTeamFinalResultConfiguration : IEntityTypeConfiguration<GameTeamFinalResult>
{
    public void Configure(EntityTypeBuilder<GameTeamFinalResult> builder)
    {
        builder.ToTable(
            "game_team_final_results",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_game_team_final_results_rounds_non_negative",
                    "rounds_played >= 0 AND penalty_total >= 0 "
                    + "AND total_kills >= 0 AND total_bounties >= 0"
                );
                table.HasCheckConstraint(
                    "ck_game_team_final_results_unplayed_semantics",
                    "(rounds_played = 0 AND best_score IS NULL AND final_score IS NULL AND placement IS NULL AND last_finished_at_utc IS NULL) OR "
                    + "(rounds_played > 0 AND best_score IS NOT NULL AND final_score IS NOT NULL AND placement IS NOT NULL AND placement > 0 AND last_finished_at_utc IS NOT NULL)"
                );
                table.HasCheckConstraint(
                    "ck_game_team_final_results_team_slot_positive",
                    "team_slot_index_snapshot > 0"
                );
            }
        );

        builder.HasKey(x => new { x.GameId, x.TeamId });
        builder.Property(x => x.TeamNameSnapshot).HasMaxLength(128);
        builder.Property(x => x.ParticipantNamesSnapshot).HasColumnType("text[]").IsRequired();

        builder
            .HasOne(x => x.Finalization)
            .WithMany(x => x.TeamResults)
            .HasForeignKey(x => x.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.Team)
            .WithMany()
            .HasForeignKey(x => new { x.GameId, x.TeamId })
            .HasPrincipalKey(x => new { x.GameId, x.Id })
            .HasConstraintName("fk_game_team_final_results_team_same_game")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.GameId, x.Placement });
    }
}
