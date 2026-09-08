using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class GameRoundParticipantConfiguration : IEntityTypeConfiguration<GameRoundParticipant>
{
    public void Configure(EntityTypeBuilder<GameRoundParticipant> builder)
    {
        builder.ToTable(
            "game_round_participants",
            table => table.HasCheckConstraint(
                "ck_game_round_participants_display_name_not_blank",
                "length(trim(display_name_snapshot)) > 0"
            )
        );

        builder.HasKey(x => x.Id);
        builder.Property(x => x.RoundId).HasColumnName("round_id");
        builder.Property(x => x.DisplayNameSnapshot).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder
            .HasIndex(x => new { x.RoundId, x.UserId })
            .IsUnique()
            .HasDatabaseName("ux_game_round_participants_round_user");
        builder
            .HasIndex(x => new { x.UserId, x.CreatedAtUtc })
            .HasDatabaseName("ix_game_round_participants_user_created");

        builder
            .HasOne(x => x.Round)
            .WithMany(x => x.Participants)
            .HasForeignKey(x => x.RoundId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
