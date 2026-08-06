using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class GameCardRunModifierResultConfiguration
    : IEntityTypeConfiguration<GameCardRunModifierResult>
{
    public void Configure(EntityTypeBuilder<GameCardRunModifierResult> builder)
    {
        builder.ToTable(
            "game_round_modifier_results",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_game_round_modifier_results_status_allowed",
                    GameCardRunModifierOutcomeValue.CheckSqlAllowedStatuses
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_round_modifier_results_resolution_semantics",
                    "((outcome_status = 'pending') AND resolved_at_utc IS NULL AND resolved_by_user_id IS NULL) "
                    + "OR ((outcome_status <> 'pending') AND resolved_at_utc IS NOT NULL AND resolved_by_user_id IS NOT NULL)"
                );
            }
        );

        builder.HasKey(x => x.Id);
        builder.Property(x => x.CardRunId).HasColumnName("round_id");
        builder.Property(x => x.GameActiveModifierId).HasColumnName("modifier_activation_id");
        builder.Property(x => x.ModifierNameSnapshot).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ModifierCategorySnapshot).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ModifierMechanicTypeSnapshot).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ModifierDescriptionSnapshot).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.ModifierScoringTypeSnapshot).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ModifierEffectSnapshotJson).HasColumnType("jsonb");
        builder.Property(x => x.OutcomeStatus).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ResolutionDataJson).HasColumnType("jsonb");
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder
            .HasIndex(x => new { x.CardRunId, x.GameActiveModifierId })
            .IsUnique()
            .HasDatabaseName("ux_game_round_modifier_results_round_activation");
        builder
            .HasIndex(x => new { x.CardRunId, x.OutcomeStatus })
            .HasDatabaseName("ix_game_round_modifier_results_round_status");
        builder
            .HasIndex(x => new { x.ModifierId, x.OutcomeStatus })
            .HasDatabaseName("ix_game_round_modifier_results_modifier_status");

        builder
            .HasOne(x => x.CardRun)
            .WithMany(x => x.ModifierResults)
            .HasForeignKey(x => x.CardRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.GameActiveModifier)
            .WithMany()
            .HasForeignKey(x => x.GameActiveModifierId)
            .HasConstraintName("fk_round_modifier_results_activation")
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
