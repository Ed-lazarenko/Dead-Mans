using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class GameCardRunParticipantConfiguration : IEntityTypeConfiguration<GameCardRunParticipant>
{
    public void Configure(EntityTypeBuilder<GameCardRunParticipant> builder)
    {
        builder.ToTable("game_round_participants");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.CardRunId).HasColumnName("round_id");
        builder.Property(x => x.DisplayNameSnapshot).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder
            .HasIndex(x => new { x.CardRunId, x.UserId })
            .IsUnique()
            .HasDatabaseName("ux_game_round_participants_round_user");
        builder
            .HasIndex(x => new { x.UserId, x.CreatedAtUtc })
            .HasDatabaseName("ix_game_round_participants_user_created");

        builder
            .HasOne(x => x.CardRun)
            .WithMany(x => x.Participants)
            .HasForeignKey(x => x.CardRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
