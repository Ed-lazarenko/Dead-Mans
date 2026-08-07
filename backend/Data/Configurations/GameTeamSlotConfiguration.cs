using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class GameTeamSlotConfiguration : IEntityTypeConfiguration<GameTeamSlot>
{
    public void Configure(EntityTypeBuilder<GameTeamSlot> builder)
    {
        builder.ToTable(
            "game_team_slots",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_game_team_slots_availability",
                    SlotAvailabilityValue.CheckSqlAllowed
                );
            }
        );

        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.GameId, x.Id });
        builder.Property(x => x.SlotIndex).IsRequired();
        builder.Property(x => x.Availability).HasMaxLength(16).IsRequired();
        builder.Property(x => x.ReservedLabel).HasMaxLength(200);
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.GameId, x.SlotIndex }).IsUnique();
        builder.HasIndex(x => new { x.GameId, x.Availability });

        builder
            .HasOne(x => x.Game)
            .WithMany(x => x.TeamSlots)
            .HasForeignKey(x => x.GameId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
