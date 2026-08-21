using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class GameModifierActivationConfiguration : IEntityTypeConfiguration<GameModifierActivation>
{
    public void Configure(EntityTypeBuilder<GameModifierActivation> builder)
    {
        builder.ToTable(
            "game_modifier_activations",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_game_modifier_activations_cost_snapshot_non_negative",
                    "activation_cost_snapshot >= 0"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_modifier_activations_definition_revision_positive",
                    "definition_revision_snapshot >= 1"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_modifier_activations_behavior_v2_schema",
                    "behavior_v2_snapshot_json ->> 'schemaVersion' = '2'"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_modifier_activations_status_allowed",
                    GameModifierActivationStatusValue.CheckSqlAllowedStatuses
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_modifier_activations_refund_range",
                    "refund_amount >= 0 AND refund_amount <= activation_cost_snapshot"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_modifier_activations_lifecycle_semantics",
                    "(status = 'active' AND archived_at_utc IS NULL AND cancelled_at_utc IS NULL "
                    + "AND cancelled_by_user_id IS NULL AND cancellation_reason IS NULL AND refund_amount = 0) "
                    + "OR (status = 'consumed' AND cancelled_at_utc IS NULL "
                    + "AND cancelled_by_user_id IS NULL AND cancellation_reason IS NULL AND refund_amount = 0) "
                    + "OR (status = 'cancelled' AND archived_at_utc IS NOT NULL AND cancelled_at_utc IS NOT NULL "
                    + "AND cancelled_by_user_id IS NOT NULL AND refund_amount = activation_cost_snapshot)"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_modifier_activations_timestamp_order",
                    "(archived_at_utc IS NULL OR archived_at_utc >= activated_at_utc) "
                    + "AND (cancelled_at_utc IS NULL OR (cancelled_at_utc >= activated_at_utc AND archived_at_utc = cancelled_at_utc))"
                );
            }
        );

        builder.HasKey(x => x.Id);
        builder.Property(x => x.RoundId).IsRequired();
        builder.Property(x => x.ModifierId).IsRequired();
        builder.Property(x => x.ActivationCostSnapshot).IsRequired();
        builder.Property(x => x.DefinitionRevisionSnapshot).IsRequired();
        builder.Property(x => x.ModifierNameSnapshot).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ModifierDescriptionSnapshot).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.ModifierCategorySnapshot).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ModifierIconEmojiSnapshot).HasMaxLength(16);
        builder.Property(x => x.ActivationCommandSnapshot).HasMaxLength(128);
        builder.Property(x => x.NormalizedTagsSnapshot).HasColumnType("text[]").IsRequired();
        builder.Property(x => x.BehaviorV2SnapshotJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.ActivatedAtUtc).IsRequired();
        builder.Property(x => x.ActivatedByUserId).IsRequired();
        builder.Property(x => x.InitiatedByUserId).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(16).IsRequired();
        builder.Property(x => x.ArchivedAtUtc);
        builder.Property(x => x.CancelledByUserId);
        builder.Property(x => x.CancelledAtUtc);
        builder.Property(x => x.CancellationReason).HasMaxLength(1000);
        builder.Property(x => x.RefundAmount).IsRequired();

        builder
            .HasIndex(x => new { x.GameId, x.ModifierId })
            .HasDatabaseName("ix_game_modifier_activations_game_modifier");
        builder
            .HasIndex(x => new { x.GameId, x.ActivatedAtUtc })
            .HasDatabaseName("ix_game_modifier_activations_game_activated");
        builder
            .HasIndex(x => new { x.GameId, x.ArchivedAtUtc })
            .HasDatabaseName("ix_game_modifier_activations_game_archived");
        builder
            .HasIndex(x => new { x.RoundId, x.Status, x.ActivatedAtUtc })
            .HasDatabaseName("ix_game_modifier_activations_round_status_activated");
        builder
            .HasIndex(x => new { x.ActivatedByUserId, x.ActivatedAtUtc })
            .HasDatabaseName("ix_game_modifier_activations_user_activated");

        builder.HasOne(x => x.Game)
            .WithMany(x => x.ModifierActivations)
            .HasForeignKey(x => x.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ModifierDefinition)
            .WithMany(x => x.GameActivations)
            .HasForeignKey(x => x.ModifierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Round)
            .WithMany()
            .HasForeignKey(x => x.RoundId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ActivatedByUser)
            .WithMany(x => x.ActivatedGameModifiers)
            .HasForeignKey(x => x.ActivatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.InitiatedByUser)
            .WithMany()
            .HasForeignKey(x => x.InitiatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CancelledByUser)
            .WithMany()
            .HasForeignKey(x => x.CancelledByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
