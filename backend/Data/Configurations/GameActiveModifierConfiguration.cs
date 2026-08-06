using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class GameActiveModifierConfiguration : IEntityTypeConfiguration<GameActiveModifier>
{
    public void Configure(EntityTypeBuilder<GameActiveModifier> builder)
    {
        builder.ToTable(
            "game_modifier_activations",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_game_modifier_activations_cost_snapshot_non_negative",
                    "activation_cost_snapshot >= 0"
                );
            }
        );

        builder.HasKey(x => x.Id);
        builder.Property(x => x.ModifierId).IsRequired();
        builder.Property(x => x.ActivationCostSnapshot).IsRequired();
        builder.Property(x => x.ActivatedAtUtc).IsRequired();
        builder.Property(x => x.ActivatedByUserId).IsRequired();
        builder.Property(x => x.ArchivedAtUtc);

        builder
            .HasIndex(x => new { x.GameId, x.ModifierId })
            .HasDatabaseName("ix_modifier_activations_game_modifier");
        builder
            .HasIndex(x => new { x.GameId, x.ActivatedAtUtc })
            .HasDatabaseName("ix_modifier_activations_game_activated");
        builder
            .HasIndex(x => new { x.GameId, x.ArchivedAtUtc })
            .HasDatabaseName("ix_modifier_activations_game_archived");
        builder
            .HasIndex(x => new { x.ActivatedByUserId, x.ActivatedAtUtc })
            .HasDatabaseName("ix_modifier_activations_user_activated");

        builder.HasOne(x => x.Game)
            .WithMany(x => x.ActiveModifiers)
            .HasForeignKey(x => x.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ModifierDefinition)
            .WithMany(x => x.GameActivations)
            .HasForeignKey(x => x.ModifierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ActivatedByUser)
            .WithMany(x => x.ActivatedGameModifiers)
            .HasForeignKey(x => x.ActivatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
