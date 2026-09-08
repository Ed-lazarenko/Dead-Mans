using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.ToTable(
            "games",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_games_status_allowed",
                    GameStatusValue.CheckSqlAllowedStatuses
                );
                tableBuilder.HasCheckConstraint(
                    "ck_games_finished_at_semantics",
                    GameStatusValue.CheckSqlFinishedAtSemantics
                );
                tableBuilder.HasCheckConstraint(
                    "ck_games_lifecycle_timestamps",
                    GameStatusValue.CheckSqlLifecycleTimestampSemantics
                );
                tableBuilder.HasCheckConstraint(
                    "ck_games_team_size_limits",
                    GameStatusValue.CheckSqlTeamSizeLimits
                );
                tableBuilder.HasCheckConstraint(
                    "ck_games_quiz_answer_duration",
                    "quiz_answer_duration_seconds BETWEEN 5 AND 3600"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_games_soft_delete_semantics",
                    "(is_deleted = FALSE AND deleted_at_utc IS NULL) OR (is_deleted = TRUE AND deleted_at_utc IS NOT NULL)"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_games_active_team_requires_active_game",
                    "(active_team_id IS NULL) OR (status = 'active' AND is_deleted = FALSE)"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_games_title_not_blank",
                    "length(trim(title)) > 0"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_games_timestamp_order",
                    "(ready_at_utc IS NULL OR ready_at_utc >= created_at_utc) "
                    + "AND (started_at_utc IS NULL OR started_at_utc >= ready_at_utc) "
                    + "AND (finished_at_utc IS NULL OR finished_at_utc >= started_at_utc) "
                    + "AND (deleted_at_utc IS NULL OR deleted_at_utc >= created_at_utc)"
                );
            }
        );

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.DeletedAtUtc);
        builder.Property(x => x.MinPlayersPerTeam).HasDefaultValue((short)1);
        builder.Property(x => x.MaxPlayersPerTeam).HasDefaultValue((short)2);
        builder.Property(x => x.QuizAnswerDurationSeconds).HasDefaultValue(60).IsRequired();
        builder.Property(x => x.ActiveTeamId);

        builder
            .HasOne(x => x.ActiveTeam)
            .WithMany()
            .HasForeignKey(x => new { x.Id, x.ActiveTeamId })
            .HasPrincipalKey(x => new { x.GameId, x.Id })
            .HasConstraintName("fk_games_active_team_same_game")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.IsDeleted, x.Status, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.Id, x.ActiveTeamId }).HasDatabaseName("ix_games_active_team_same_game");
        builder
            .HasIndex(x => x.IsDeleted, "ux_games_single_draft")
            .IsUnique()
            .HasFilter($"is_deleted = FALSE AND status = '{GameStatusValue.Draft}'");
        builder
            .HasIndex(x => x.IsDeleted, "ux_games_single_current")
            .IsUnique()
            .HasFilter(
                $"is_deleted = FALSE AND status IN ('{GameStatusValue.Ready}',"
                + $"'{GameStatusValue.Active}')"
            );
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}
