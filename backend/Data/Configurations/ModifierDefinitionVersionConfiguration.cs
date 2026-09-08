using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public sealed class ModifierDefinitionVersionConfiguration
    : IEntityTypeConfiguration<ModifierDefinitionVersion>
{
    public void Configure(EntityTypeBuilder<ModifierDefinitionVersion> builder)
    {
        builder.ToTable("modifier_definition_versions", table =>
        {
            table.HasCheckConstraint("ck_modifier_definition_versions_revision_positive", "revision >= 1");
            table.HasCheckConstraint("ck_modifier_definition_versions_cost_non_negative", "activation_cost >= 0");
            table.HasCheckConstraint("ck_modifier_definition_versions_limit_positive_or_null", "max_activations_per_round IS NULL OR max_activations_per_round > 0");
            table.HasCheckConstraint("ck_modifier_definition_versions_category_allowed", "category IN ('preparation','round','result')");
            table.HasCheckConstraint("ck_modifier_definition_versions_behavior_v2_schema", "jsonb_typeof(behavior_v2_json) = 'object' AND behavior_v2_json ->> 'schemaVersion' = '2'");
            table.HasCheckConstraint("ck_modifier_definition_versions_change_type", ModifierVersionChangeTypeValue.CheckSql);
            table.HasCheckConstraint("ck_modifier_definition_versions_change_note", "change_note IS NULL OR length(btrim(change_note)) BETWEEN 1 AND 500");
            table.HasCheckConstraint("ck_modifier_definition_versions_content_not_blank", "length(btrim(name)) > 0 AND length(btrim(description)) > 0 AND length(btrim(created_by_display_name_snapshot)) > 0");
        });

        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.ModifierId, x.Id });
        builder.HasIndex(x => new { x.ModifierId, x.Revision }).IsUnique();
        builder.HasIndex(x => new { x.ModifierId, x.CreatedAtUtc, x.Id });
        builder.HasIndex(x => x.Name, "ix_modifier_versions_name_trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
        builder.HasIndex(x => x.Category, "ix_modifier_versions_category_trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(32).IsRequired();
        builder.Property(x => x.IconEmoji).HasMaxLength(16);
        builder.Property(x => x.ActivationCommand).HasMaxLength(128);
        builder.Property(x => x.NormalizedTags).HasColumnType("text[]").IsRequired();
        builder.Property(x => x.BehaviorV2Json).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CreatedByDisplayNameSnapshot).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ChangeNote).HasMaxLength(500);
        builder.Property(x => x.ChangeType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ChangedFields).HasColumnType("text[]").IsRequired();

        builder.HasOne(x => x.Modifier)
            .WithMany(x => x.Versions)
            .HasForeignKey(x => x.ModifierId)
            .HasConstraintName("fk_modifier_versions_definition")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CascadeSourceModifier)
            .WithMany()
            .HasForeignKey(x => x.CascadeSourceModifierId)
            .HasConstraintName("fk_modifier_versions_cascade_source")
            .OnDelete(DeleteBehavior.Restrict);

    }
}
