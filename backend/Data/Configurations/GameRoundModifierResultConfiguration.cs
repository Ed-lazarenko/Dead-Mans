using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class GameRoundModifierResultConfiguration
    : IEntityTypeConfiguration<GameRoundModifierResult>
{
    public void Configure(EntityTypeBuilder<GameRoundModifierResult> builder)
    {
        builder.ToTable(
            "game_round_modifier_results",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_game_round_modifier_results_status_allowed",
                    GameRoundModifierOutcomeValue.CheckSqlAllowedStatuses
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_round_modifier_results_resolution_semantics",
                    "((outcome_status = 'pending') AND resolved_at_utc IS NULL AND resolved_by_user_id IS NULL) "
                    + "OR ((outcome_status <> 'pending') AND resolved_at_utc IS NOT NULL AND resolved_by_user_id IS NOT NULL)"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_round_modifier_results_definition_revision_positive",
                    "definition_revision_snapshot >= 1"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_round_modifier_results_behavior_v2_schema",
                    "jsonb_typeof(modifier_behavior_v2_snapshot_json) = 'object' "
                    + "AND modifier_behavior_v2_snapshot_json ->> 'schemaVersion' = '2'"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_round_modifier_results_json_objects",
                    "(resolution_data_json IS NULL OR jsonb_typeof(resolution_data_json) = 'object') "
                    + "AND (calculation_breakdown_json IS NULL OR jsonb_typeof(calculation_breakdown_json) = 'object')"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_round_modifier_results_snapshot_not_blank",
                    "length(trim(modifier_name_snapshot)) > 0 "
                    + "AND length(trim(modifier_description_snapshot)) > 0 "
                    + "AND length(trim(modifier_category_snapshot)) > 0"
                );
            }
        );

        builder.HasKey(x => x.Id);
        builder.Property(x => x.RoundId).HasColumnName("round_id");
        builder.Property(x => x.GameModifierActivationId).HasColumnName("modifier_activation_id");
        builder.Property(x => x.ModifierNameSnapshot).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ModifierCategorySnapshot).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ModifierDescriptionSnapshot).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.DefinitionRevisionSnapshot).IsRequired();
        builder.Property(x => x.ModifierActivationCommandSnapshot).HasMaxLength(128);
        builder.Property(x => x.ModifierNormalizedTagsSnapshot).HasColumnType("text[]").IsRequired();
        builder.Property(x => x.ModifierBehaviorV2SnapshotJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.OutcomeStatus).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ResolutionDataJson).HasColumnType("jsonb");
        builder.Property(x => x.ResolutionGroupId);
        builder.Property(x => x.ResolutionKind).HasMaxLength(32);
        builder.Property(x => x.ViolationComment).HasMaxLength(1000);
        builder.Property(x => x.CalculationBreakdownJson).HasColumnType("jsonb");
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder
            .HasIndex(x => new { x.RoundId, x.GameModifierActivationId })
            .IsUnique()
            .HasDatabaseName("ux_game_round_modifier_results_round_activation");
        builder
            .HasIndex(x => new { x.RoundId, x.OutcomeStatus })
            .HasDatabaseName("ix_game_round_modifier_results_round_status");
        builder
            .HasIndex(x => new { x.ModifierId, x.OutcomeStatus })
            .HasDatabaseName("ix_game_round_modifier_results_modifier_status");
        builder
            .HasIndex(x => new
            {
                x.RoundId,
                x.GameModifierActivationId,
                x.ModifierId
            })
            .HasDatabaseName("ix_round_modifier_results_activation_fk");

        builder
            .HasOne(x => x.Round)
            .WithMany(x => x.ModifierResults)
            .HasForeignKey(x => x.RoundId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.GameModifierActivation)
            .WithMany()
            .HasForeignKey(x => new
            {
                x.RoundId,
                x.GameModifierActivationId,
                x.ModifierId
            })
            .HasPrincipalKey(x => new { x.RoundId, x.Id, x.ModifierId })
            .HasConstraintName(
                "fk_modifier_results_activation_same_round_modifier"
            )
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.ModifierDefinition)
            .WithMany()
            .HasForeignKey(x => x.ModifierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.ResolvedByUser)
            .WithMany()
            .HasForeignKey(x => x.ResolvedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
