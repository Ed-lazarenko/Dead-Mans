using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class GameTeamInvitationConfiguration
    : IEntityTypeConfiguration<GameTeamInvitation>
{
    public void Configure(EntityTypeBuilder<GameTeamInvitation> builder)
    {
        builder.ToTable(
            "game_team_invitations",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_game_team_invitations_status",
                    TeamInvitationStatusValue.CheckSqlAllowedStatuses
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_team_invitations_invited_by_kind",
                    InvitedByKindValue.CheckSqlAllowed
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_team_invitations_response_timestamp_semantics",
                    "((status = 'pending') AND responded_at_utc IS NULL) OR "
                    + "((status <> 'pending') AND responded_at_utc IS NOT NULL "
                    + "AND responded_at_utc >= created_at_utc)"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_team_invitations_source_team_semantics",
                    "invited_by_kind = 'admin' OR team_id IS NOT NULL"
                );
            }
        );

        builder.HasKey(x => x.Id);
        builder.Property(x => x.InvitedByKind).HasMaxLength(16).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(16).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.GameId, x.Status });
        builder
            .HasIndex(x => new { x.GameId, x.SlotId })
            .HasDatabaseName("ix_game_team_invitations_game_slot");
        builder
            .HasIndex(x => new { x.GameId, x.TeamId })
            .HasDatabaseName("ix_game_team_invitations_game_team");
        builder.HasIndex(x => new { x.InvitedUserId, x.Status });
        builder
            .HasIndex(x => new { x.GameId, x.InvitedUserId })
            .IsUnique()
            .HasFilter($"status = '{TeamInvitationStatusValue.Pending}'")
            .HasDatabaseName("ux_game_team_invitations_one_pending_per_user");

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
            .HasOne(x => x.Team)
            .WithMany()
            .HasForeignKey(x => new { x.GameId, x.TeamId })
            .HasPrincipalKey(x => new { x.GameId, x.Id })
            .HasConstraintName("fk_game_team_invitations_team_same_game")
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.InvitedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.InvitedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
