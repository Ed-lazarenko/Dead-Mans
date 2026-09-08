using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public sealed class ModifierDefinitionVersionConflictConfiguration
    : IEntityTypeConfiguration<ModifierDefinitionVersionConflict>
{
    public void Configure(EntityTypeBuilder<ModifierDefinitionVersionConflict> builder)
    {
        builder.ToTable(
            "modifier_definition_version_conflicts",
            table => table.HasCheckConstraint(
                "ck_modifier_definition_version_conflicts_name_not_blank",
                "length(trim(conflicting_modifier_name_snapshot)) > 0"
            )
        );
        builder.HasKey(x => new { x.ModifierVersionId, x.ConflictingModifierId });
        builder.Property(x => x.ConflictingModifierNameSnapshot).HasMaxLength(128).IsRequired();
        builder
            .HasIndex(x => x.ConflictingModifierId)
            .HasDatabaseName("ix_modifier_conflicts_definition");
        builder.HasOne(x => x.ModifierVersion)
            .WithMany(x => x.Conflicts)
            .HasForeignKey(x => x.ModifierVersionId)
            .HasConstraintName("fk_modifier_conflicts_version")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ConflictingModifier)
            .WithMany()
            .HasForeignKey(x => x.ConflictingModifierId)
            .HasConstraintName("fk_modifier_conflicts_definition")
            .OnDelete(DeleteBehavior.Restrict);

    }
}
