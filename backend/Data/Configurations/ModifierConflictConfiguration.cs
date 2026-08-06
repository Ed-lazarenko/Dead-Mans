using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class ModifierConflictConfiguration : IEntityTypeConfiguration<ModifierConflict>
{
    public void Configure(EntityTypeBuilder<ModifierConflict> builder)
    {
        builder.ToTable(
            "modifier_conflicts",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_modifier_conflicts_distinct_ids",
                    "modifier_id <> conflicts_with_modifier_id"
                );
            }
        );

        builder.HasKey(x => new { x.ModifierId, x.ConflictsWithModifierId });
        builder.Property(x => x.ModifierId).IsRequired();
        builder.Property(x => x.ConflictsWithModifierId).IsRequired();

        builder.HasOne(x => x.Modifier)
            .WithMany()
            .HasForeignKey(x => x.ModifierId)
            .HasConstraintName("fk_modifier_conflicts_modifier")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ConflictsWithModifier)
            .WithMany()
            .HasForeignKey(x => x.ConflictsWithModifierId)
            .HasConstraintName("fk_modifier_conflicts_conflicting_modifier")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(
            Pair(ModifierDefinitionSeedIds.Prokaznik, ModifierDefinitionSeedIds.Mentorbait),
            Pair(ModifierDefinitionSeedIds.Prokaznik, ModifierDefinitionSeedIds.Krysa),
            Pair(ModifierDefinitionSeedIds.Prokaznik, ModifierDefinitionSeedIds.Shot),
            Pair(ModifierDefinitionSeedIds.Mentorbait, ModifierDefinitionSeedIds.Krysa)
        );
    }

    private static ModifierConflict Pair(Guid left, Guid right) =>
        new() { ModifierId = left, ConflictsWithModifierId = right };
}
