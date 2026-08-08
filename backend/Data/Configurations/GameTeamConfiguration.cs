using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class GameTeamConfiguration : IEntityTypeConfiguration<GameTeam>
{
    public void Configure(EntityTypeBuilder<GameTeam> builder)
    {
        builder.ToTable(
            "game_teams",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_game_teams_status_allowed",
                    TeamStatusValue.CheckSqlAllowedStatuses
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_teams_status_timestamp_semantics",
                    "((status = 'forming') AND confirmed_at_utc IS NULL AND rejected_at_utc IS NULL AND disbanded_at_utc IS NULL AND disband_requested_at_utc IS NULL) "
                    + "OR ((status = 'confirmed') AND confirmed_at_utc IS NOT NULL AND confirmed_by_user_id IS NOT NULL AND rejected_at_utc IS NULL AND disbanded_at_utc IS NULL) "
                    + "OR ((status = 'rejected') AND rejected_at_utc IS NOT NULL AND rejected_by_user_id IS NOT NULL AND disbanded_at_utc IS NULL AND disband_requested_at_utc IS NULL) "
                    + "OR ((status = 'disbanded') AND disbanded_at_utc IS NOT NULL AND disbanded_by_user_id IS NOT NULL AND disband_requested_at_utc IS NULL)"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_teams_disband_request_user_pair",
                    "(disband_requested_at_utc IS NULL AND disband_requested_by_user_id IS NULL) OR (disband_requested_at_utc IS NOT NULL AND disband_requested_by_user_id IS NOT NULL)"
                );
            }
        );

        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.GameId, x.Id });
        builder.Property(x => x.Name).HasMaxLength(TeamNameValue.MaxLength);
        builder.Property(x => x.RecruitmentOpen).IsRequired();
        builder.Property(x => x.IsPlayed).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder
            .HasIndex(x => x.SlotId, "ux_game_teams_active_slot")
            .IsUnique()
            .HasFilter(TeamStatusValue.CheckSqlOccupyingStatuses);
        builder.HasIndex(x => new { x.GameId, x.Status });

        builder
            .HasOne(x => x.Game)
            .WithMany()
            .HasForeignKey(x => x.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.Slot)
            .WithMany()
            .HasForeignKey(x => new { x.GameId, x.SlotId })
            .HasPrincipalKey(x => new { x.GameId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ConfirmedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.RejectedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.DisbandedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.DisbandRequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
