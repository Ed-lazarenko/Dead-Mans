using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class GameTeamMemberConfiguration : IEntityTypeConfiguration<GameTeamMember>
{
    public void Configure(EntityTypeBuilder<GameTeamMember> builder)
    {
        builder.ToTable(
            "game_team_members",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_game_team_members_left_after_join",
                    "left_at_utc IS NULL OR left_at_utc >= joined_at_utc"
                );
            }
        );

        builder.HasKey(x => x.Id);
        builder.Property(x => x.JoinedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.TeamId, x.UserId });
        builder
            .HasIndex(x => new { x.TeamId, x.UserId }, "ux_game_team_members_active_team_user")
            .IsUnique()
            .HasFilter("left_at_utc IS NULL");
        builder
            .HasIndex(x => new { x.GameId, x.UserId }, "ux_game_team_members_active_game_user")
            .IsUnique()
            .HasFilter("left_at_utc IS NULL");

        builder
            .HasOne(x => x.Team)
            .WithMany(x => x.Members)
            .HasForeignKey(x => new { x.GameId, x.TeamId })
            .HasPrincipalKey(x => new { x.GameId, x.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<Game>()
            .WithMany()
            .HasForeignKey(x => x.GameId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
