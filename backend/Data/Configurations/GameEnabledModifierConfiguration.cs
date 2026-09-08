using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class GameEnabledModifierConfiguration : IEntityTypeConfiguration<GameEnabledModifier>
{
    public void Configure(EntityTypeBuilder<GameEnabledModifier> builder)
    {
        builder.HasKey(x => new { x.GameId, x.ModifierId });
        builder.Property(x => x.ModifierId).IsRequired();
        builder.Property(x => x.ModifierVersionId);
        builder.Property(x => x.VersionPinnedAtUtc);
        builder.Property(x => x.EnabledAtUtc).IsRequired();
        builder.Property(x => x.EmergencyDisableReason).HasMaxLength(1000);

        builder.ToTable(
            "game_enabled_modifiers",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_game_enabled_modifiers_emergency_disable_audit",
                    "(emergency_disabled_at_utc IS NULL AND emergency_disabled_by_user_id IS NULL AND emergency_disable_reason IS NULL) OR "
                        + "(emergency_disabled_at_utc IS NOT NULL AND emergency_disabled_by_user_id IS NOT NULL AND emergency_disable_reason IS NOT NULL "
                        + "AND length(btrim(emergency_disable_reason)) BETWEEN 1 AND 1000 AND emergency_disabled_at_utc >= enabled_at_utc)"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_enabled_modifiers_version_pin_pair",
                    "(modifier_version_id IS NULL AND version_pinned_at_utc IS NULL) OR "
                    + "(modifier_version_id IS NOT NULL AND version_pinned_at_utc IS NOT NULL "
                    + "AND version_pinned_at_utc >= enabled_at_utc)"
                );
            }
        );

        builder.HasIndex(x => new { x.ModifierVersionId, x.GameId });

        builder.HasOne(x => x.Game)
            .WithMany(x => x.EnabledModifiers)
            .HasForeignKey(x => x.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ModifierDefinition)
            .WithMany(x => x.EnabledInGames)
            .HasForeignKey(x => x.ModifierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ModifierVersion)
            .WithMany()
            .HasForeignKey(x => new { x.ModifierId, x.ModifierVersionId })
            .HasPrincipalKey(x => new { x.ModifierId, x.Id })
            .HasConstraintName("fk_game_enabled_modifiers_modifier_version")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.EmergencyDisabledByUser)
            .WithMany()
            .HasForeignKey(x => x.EmergencyDisabledByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
