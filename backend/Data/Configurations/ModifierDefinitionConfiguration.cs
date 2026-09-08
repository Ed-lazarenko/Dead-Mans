using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

/// <summary>
/// Maps the stable modifier identity. Mutable game content is stored only in immutable versions.
/// </summary>
public sealed class ModifierDefinitionConfiguration : IEntityTypeConfiguration<ModifierDefinition>
{
    public void Configure(EntityTypeBuilder<ModifierDefinition> builder)
    {
        builder.ToTable(
            "modifier_definitions",
            table => table.HasCheckConstraint(
                "ck_modifier_definitions_archive_semantics",
                "(is_archived = FALSE AND archived_at_utc IS NULL AND archived_by_user_id IS NULL) OR "
                + "(is_archived = TRUE AND archived_at_utc IS NOT NULL AND archived_by_user_id IS NOT NULL "
                + "AND archived_at_utc >= created_at_utc)"
            )
        );
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.IsArchived).HasDefaultValue(false);
        builder.Property(x => x.CurrentVersionId);
        builder.Property(x => x.CreatedByUserId);
        builder.Property(x => x.ArchivedAtUtc);
        builder.Property(x => x.ArchivedByUserId);
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasIndex(x => x.CurrentVersionId).IsUnique();
        builder.HasIndex(x => new { x.IsArchived, x.CreatedAtUtc, x.Id })
            .IsDescending(false, true, true);
        builder.HasIndex(x => new { x.CreatedAtUtc, x.Id })
            .IsDescending(true, true);
        builder.HasOne(x => x.CurrentVersion)
            .WithMany()
            .HasForeignKey(x => new { x.Id, x.CurrentVersionId })
            .HasPrincipalKey(x => new { x.ModifierId, x.Id })
            .HasConstraintName("fk_modifier_definitions_current_version")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ArchivedByUser)
            .WithMany()
            .HasForeignKey(x => x.ArchivedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
