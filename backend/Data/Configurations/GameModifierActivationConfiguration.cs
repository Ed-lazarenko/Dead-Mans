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
                    "jsonb_typeof(behavior_v2_snapshot_json) = 'object' "
                    + "AND behavior_v2_snapshot_json ->> 'schemaVersion' = '2'"
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
                tableBuilder.HasCheckConstraint(
                    "ck_game_modifier_activations_snapshot_not_blank",
                    "length(trim(modifier_name_snapshot)) > 0 "
                    + "AND length(trim(modifier_description_snapshot)) > 0 "
                    + "AND length(trim(modifier_category_snapshot)) > 0"
                );
            }
        );

        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.GameId, x.Id });
        builder.HasAlternateKey(x => new { x.RoundId, x.Id, x.ModifierId });
        builder.Property(x => x.RoundId).IsRequired();
        builder.Property(x => x.ModifierId).IsRequired();
        builder.Property(x => x.ModifierVersionId).IsRequired();
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
            .HasIndex(x => new { x.ModifierVersionId, x.GameId })
            .HasDatabaseName("ix_game_modifier_activations_version_game");
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

        builder.HasOne(x => x.ModifierVersion)
            .WithMany()
            .HasForeignKey(x => new { x.ModifierId, x.ModifierVersionId })
            .HasPrincipalKey(x => new { x.ModifierId, x.Id })
            .HasConstraintName("fk_game_modifier_activations_modifier_version")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.EnabledModifier)
            .WithMany()
            .HasForeignKey(x => new { x.GameId, x.ModifierId })
            .HasPrincipalKey(x => new { x.GameId, x.ModifierId })
            .HasConstraintName("fk_game_modifier_activations_enabled_modifier")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Round)
            .WithMany()
            .HasForeignKey(x => new { x.GameId, x.RoundId })
            .HasPrincipalKey(x => new { x.GameId, x.Id })
            .HasConstraintName("fk_modifier_activations_game_rounds_same_game")
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
