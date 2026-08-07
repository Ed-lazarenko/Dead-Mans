using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class GameEnabledModifierConfiguration : IEntityTypeConfiguration<GameEnabledModifier>
{
    public void Configure(EntityTypeBuilder<GameEnabledModifier> builder)
    {
        builder.ToTable(
            "game_enabled_modifiers",
            tableBuilder =>
            {
            }
        );

        builder.HasKey(x => new { x.GameId, x.ModifierId });
        builder.Property(x => x.ModifierId).IsRequired();
        builder.Property(x => x.EnabledAtUtc).IsRequired();

        builder.HasIndex(x => x.GameId);

        builder.HasOne(x => x.Game)
            .WithMany(x => x.EnabledModifiers)
            .HasForeignKey(x => x.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ModifierDefinition)
            .WithMany(x => x.EnabledInGames)
            .HasForeignKey(x => x.ModifierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
