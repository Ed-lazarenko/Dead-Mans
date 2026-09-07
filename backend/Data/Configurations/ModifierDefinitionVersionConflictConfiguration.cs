using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public sealed class ModifierDefinitionVersionConflictConfiguration
    : IEntityTypeConfiguration<ModifierDefinitionVersionConflict>
{
    public void Configure(EntityTypeBuilder<ModifierDefinitionVersionConflict> builder)
    {
        builder.ToTable("modifier_definition_version_conflicts");
        builder.HasKey(x => new { x.ModifierVersionId, x.ConflictingModifierId });
        builder.Property(x => x.ConflictingModifierNameSnapshot).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.ConflictingModifierId);
        builder.HasOne(x => x.ModifierVersion)
            .WithMany(x => x.Conflicts)
            .HasForeignKey(x => x.ModifierVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ConflictingModifier)
            .WithMany()
            .HasForeignKey(x => x.ConflictingModifierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
