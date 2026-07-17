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
                    "CK_games_status_allowed",
                    GameStatusValue.CheckSqlAllowedStatuses
                );
                tableBuilder.HasCheckConstraint(
                    "CK_games_finishedat_semantics",
                    GameStatusValue.CheckSqlFinishedAtSemantics
                );
                tableBuilder.HasCheckConstraint(
                    "CK_games_lifecycle_timestamps",
                    GameStatusValue.CheckSqlLifecycleTimestampSemantics
                );
                tableBuilder.HasCheckConstraint(
                    "CK_games_team_size_limits",
                    GameStatusValue.CheckSqlTeamSizeLimits
                );
                tableBuilder.HasCheckConstraint(
                    "CK_games_soft_delete_semantics",
                    "(\"IsDeleted\" = FALSE AND \"DeletedAtUtc\" IS NULL) OR (\"IsDeleted\" = TRUE AND \"DeletedAtUtc\" IS NOT NULL)"
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
        builder.Property(x => x.ActiveTeamId);

        builder
            .HasOne(x => x.ActiveTeam)
            .WithMany()
            .HasForeignKey(x => x.ActiveTeamId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.IsDeleted, x.Status, x.CreatedAtUtc });
        builder.HasIndex(x => x.ActiveTeamId);
        builder
            .HasIndex(x => x.Status, "UX_games_single_draft")
            .IsUnique()
            .HasFilter($"\"Status\" = '{GameStatusValue.Draft}' AND \"IsDeleted\" = FALSE");
        builder
            .HasIndex(x => x.Status, "UX_games_single_ready")
            .IsUnique()
            .HasFilter($"\"Status\" = '{GameStatusValue.Ready}' AND \"IsDeleted\" = FALSE");
        builder
            .HasIndex(x => x.Status, "UX_games_single_active")
            .IsUnique()
            .HasFilter($"\"Status\" = '{GameStatusValue.Active}' AND \"IsDeleted\" = FALSE");
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}
